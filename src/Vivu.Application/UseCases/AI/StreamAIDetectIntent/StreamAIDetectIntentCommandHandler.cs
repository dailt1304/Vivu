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
using Vivu.Application.UseCases.AI.StreamChatWithAI;
using Vivu.Application.UseCases.AI.StreamModifyTrip;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.AI.StreamAIChat
{
    public class StreamAIDetectIntentCommandHandler
    : IStreamRequestHandler<StreamAIDetectIntentCommand, Result<StreamEvent>>
    {
        private readonly IIntentDetectionService _intentDetection;
        private readonly IMediator _mediator;
        private readonly IChatMessageRepository _chatMessageRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<StreamAIDetectIntentCommandHandler> _logger;

        public StreamAIDetectIntentCommandHandler(
            IIntentDetectionService intentDetection,
            IMediator mediator,
            IChatMessageRepository chatMessageRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            ILogger<StreamAIDetectIntentCommandHandler> logger)
        {
            _intentDetection = intentDetection;
            _mediator = mediator;
            _chatMessageRepository = chatMessageRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async IAsyncEnumerable<Result<StreamEvent>> Handle(
            StreamAIDetectIntentCommand request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
            {
                yield return Result<StreamEvent>.Failure(DomainErrors.Auth.InvalidToken);
                yield break;
            }

            yield return Result<StreamEvent>.Success(new StreamEvent
            {
                Type = StreamEventType.Parsing,
                Data = new { message = "VivuAI đang phân tích yêu cầu..." }
            });

            var intent = await _intentDetection.DetectIntentAsync(
                request.UserMessage, cancellationToken);

            _logger.LogInformation(
                "Intent detected: {Intent} for user {UserId}, trip {TripId}",
                intent, userId, request.TripId);

            if (!request.SkipSavingUserMessage)
            {
                var userChatMessage = ChatMessage.Create(
                    tripId: request.TripId,
                    senderId: userId,
                    content: request.UserMessage,
                    messageType: "text",
                    isAiMessage: false
                );
                await _chatMessageRepository.AddAsync(userChatMessage);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }


            if (intent == AIChatIntent.ModifyTrip)
            {
                var modifyCommand = new StreamModifyTripCommand
                {
                    TripId = request.TripId,
                    UserRequest = request.UserMessage
                };

                await foreach (var chunk in _mediator.CreateStream(modifyCommand, cancellationToken))
                    yield return chunk;
            }
            else
            {
                var chatCommand = new StreamChatWithAICommand
                {
                    TripId = request.TripId,
                    UserMessage = request.UserMessage
                };

                await foreach (var chunk in _mediator.CreateStream(chatCommand, cancellationToken))
                    yield return chunk;
            }
        }
    }
}
