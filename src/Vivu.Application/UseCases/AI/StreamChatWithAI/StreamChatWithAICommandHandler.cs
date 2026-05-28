using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.Interfaces.AI;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.AI.StreamChatWithAI
{
    public class StreamChatWithAICommandHandler
    : IStreamRequestHandler<StreamChatWithAICommand, Result<StreamEvent>>
    {
        private readonly IAIClientFactory _clientFactory;
        private readonly ITripRepository _tripRepository;
        private readonly IStreamingAIService _aiService;
        private readonly IChatMessageRepository _chatMessageRepository;
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;
        private readonly IUsageTrackingService _usageTracker;
        private readonly IRateLimitService _rateLimitService;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<StreamChatWithAICommandHandler> _logger;

        public StreamChatWithAICommandHandler(
            IAIClientFactory clientFactory,
            IUnitOfWork unitOfWork,
            ITripRepository tripRepository,
            IChatMessageRepository chatMessageRepository,
            IStreamingAIService aiService,
            IUserSubscriptionRepository userSubscriptionRepository,
            IUsageTrackingService usageTracker,
            IRateLimitService rateLimitService,
            ICurrentUser currentUser,
            ILogger<StreamChatWithAICommandHandler> logger)
        {
            _clientFactory = clientFactory;
            _unitOfWork = unitOfWork;
            _tripRepository = tripRepository;
            _aiService = aiService;
            _chatMessageRepository = chatMessageRepository;
            _userSubscriptionRepository = userSubscriptionRepository;
            _usageTracker = usageTracker;
            _rateLimitService = rateLimitService;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async IAsyncEnumerable<Result<StreamEvent>> Handle(
            StreamChatWithAICommand request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
            {
                yield return Result<StreamEvent>.Failure(DomainErrors.Auth.InvalidToken);
                yield break;
            }

            var rateLimitResult = await _rateLimitService.CheckLimitAsync(userId, cancellationToken);
            if (rateLimitResult.IsFailure)
            {
                yield return Result<StreamEvent>.Failure(rateLimitResult.Error);
                yield break;
            }

            yield return Result<StreamEvent>.Success(new StreamEvent
            {
                Type = StreamEventType.Start,
                Data = new { message = "ViVuAI is responding..." }
            });

            var trip = await _tripRepository.GetTripByIdWithDetailsAsync(
                request.TripId, cancellationToken);
            if (trip == null)
            {
                yield return Result<StreamEvent>.Failure(DomainErrors.Trip.NotFound);
                yield break;
            }

            var recentMessages = await _chatMessageRepository.GetRecentMessagesAsync(
                request.TripId, limit: 10, cancellationToken);


            var completeResponse = new StringBuilder();
            int totalPromptTokens = 0, totalOutputTokens = 0, responseTimeMs = 0;
            string providerName = "";

            await foreach (var chunk in _aiService.ChatMessageStreamAsync(
                trip,recentMessages,request.UserMessage,cancellationToken
                ))
            {
                if (chunk.IsFailure)
                {
                    yield return Result<StreamEvent>.Failure(chunk.Error!);
                    yield break;
                }

                var chunkData = chunk.Value!;
                completeResponse.Append(chunkData.Content);

                if (chunkData.PromptTokens.HasValue) totalPromptTokens = chunkData.PromptTokens.Value;
                if (chunkData.CompletionTokens.HasValue) totalOutputTokens = chunkData.CompletionTokens.Value;
                if (!string.IsNullOrEmpty(chunkData.ProviderName)) providerName = chunkData.ProviderName;
                if (chunkData.ResponseTimeMs.HasValue) responseTimeMs = chunkData.ResponseTimeMs.Value;

                yield return Result<StreamEvent>.Success(new StreamEvent
                {
                    Type = StreamEventType.Chunk,
                    Data = chunkData.Content
                });
            }

            var subscription = await _userSubscriptionRepository
                .GetUserActiveSubscription(userId, cancellationToken);
            var cost = _usageTracker.CalculateCost(totalPromptTokens, totalOutputTokens, providerName);
            await _usageTracker.LogUsageAsync(userId, subscription?.Id, providerName,
                totalPromptTokens, totalOutputTokens, responseTimeMs, cost, 200, null, cancellationToken);

            var aiChatMessage = ChatMessage.Create(
                tripId: request.TripId,
                senderId: userId,
                content: completeResponse.ToString(),
                messageType: "ai_response",
                isAiMessage: true
            );
            await _chatMessageRepository.AddAsync(aiChatMessage);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            yield return Result<StreamEvent>.Success(new StreamEvent
            {
                Type = StreamEventType.Complete,
                Data = new { message = completeResponse.ToString() }
            });
        }
    }

}
