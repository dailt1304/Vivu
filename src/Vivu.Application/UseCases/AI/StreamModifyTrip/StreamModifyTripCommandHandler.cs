using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.Interfaces.AI;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.AI.ApplyTripModification;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.AI.StreamModifyTrip
{
    public class StreamModifyTripCommandHandler
    : IStreamRequestHandler<StreamModifyTripCommand, Result<StreamEvent>>
    {
        private readonly IStreamingAIService _aiService;
        private readonly ITripRepository _tripRepository;
        private readonly IChatMessageRepository _chatMessageRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IMediator _mediator;
        private readonly IRateLimitService _rateLimitService;
        private readonly IUsageTrackingService _usageTracker;
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IAIParsing _parser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<StreamModifyTripCommandHandler> _logger;
        private readonly IUserPersonalizationService _personalizationService;
        private readonly ILocationZoneService _locationZoneService;

        public StreamModifyTripCommandHandler(
            IStreamingAIService aiService, ITripRepository tripRepository,
            IChatMessageRepository chatMessageRepository, ILocationRepository locationRepository, IMapper mapper,
            IMediator mediator, IRateLimitService rateLimitService, IUsageTrackingService usageTracker, 
            IUserSubscriptionRepository userSubscriptionRepository, ICurrentUser currentUser, 
            IAIParsing parser, IUnitOfWork unitOfWork, ILogger<StreamModifyTripCommandHandler> logger,
            IUserPersonalizationService personalizationService, ILocationZoneService locationZoneService)
        {
            _aiService = aiService;
            _tripRepository = tripRepository;
            _unitOfWork = unitOfWork;
            _chatMessageRepository = chatMessageRepository;
            _locationRepository = locationRepository;
            _mediator = mediator;
            _rateLimitService = rateLimitService;
            _usageTracker = usageTracker;
            _userSubscriptionRepository = userSubscriptionRepository;
            _currentUser = currentUser;
            _parser = parser;
            _mapper = mapper;
            _logger = logger;
            _personalizationService = personalizationService;
            _locationZoneService = locationZoneService;
        }

        public async IAsyncEnumerable<Result<StreamEvent>> Handle(
            StreamModifyTripCommand request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
            {
                yield return Result<StreamEvent>.Failure(DomainErrors.Auth.InvalidToken);
                yield break;
            }

            yield return Result<StreamEvent>.Success(new StreamEvent
            {
                Type = StreamEventType.Start,
                Data = new { message = "Đang bắt đầu phân tích..." }
            });

            var trip = await _tripRepository.GetTripByIdWithDetailsAsync(request.TripId, cancellationToken);
            if (trip == null)
            {
                yield return Result<StreamEvent>.Failure(DomainErrors.Trip.NotFound);
                yield break;
            }
            _logger.LogDebug("Fetched trip exist with id {tripid} and {dayscount} tripdays found", trip.Id, trip.TripDays.Count);

            var rateLimitResult = await _rateLimitService.CheckLimitAsync(userId, cancellationToken);
            if (rateLimitResult.IsFailure)
            {
                yield return Result<StreamEvent>.Failure(rateLimitResult.Error);
                yield break;
            }
            _logger.LogDebug("User {userid} have {request} request remaining", userId, rateLimitResult.Value.Remaining);

            var recentMessages = await _chatMessageRepository.GetRecentMessagesAsync(
                request.TripId, limit: 20, cancellationToken);
            _logger.LogDebug("Found {messcount} recent message", recentMessages.Count);

            var allLocations = await _locationRepository.GetAllLocationByCity(trip.CityId.Value);

            var personalization = await _personalizationService.BuildContextAsync(userId, 1, GroupCompositionType.General, cancellationToken);
            if (!string.IsNullOrWhiteSpace(trip.PersonalizationContextJson))
            {
                var savedCtx = System.Text.Json.JsonSerializer.Deserialize<UserPersonalizationContext>(trip.PersonalizationContextJson);
                if (savedCtx != null) personalization = savedCtx;
            }

            TripConstraints? constraints = null;
            if (!string.IsNullOrWhiteSpace(trip.ConstraintsJson))
            {
                var parsedConstraints = System.Text.Json.JsonSerializer.Deserialize<TripConstraints>(trip.ConstraintsJson);
                if (parsedConstraints != null && parsedConstraints.HasAnyConstraint)
                {
                    constraints = parsedConstraints;
                }
            }

            var currentTripLocations = trip.TripDays?.SelectMany(d => d.TripLocations?.Select(l => l.Location) ?? Array.Empty<Domain.Entities.Location>()).Where(l => l != null).ToList() ?? new List<Domain.Entities.Location>();
            var availableLocations = _locationZoneService.SelectLocationsForModify(allLocations, currentTripLocations!, request.UserRequest, personalization, constraints);
            var allLocationsForPrompt = currentTripLocations
                .Concat(availableLocations)
                .DistinctBy(l => l.Id)
                .ToList();

            var locationMap = allLocationsForPrompt
                .Select((loc, idx) => new { Index = idx + 1, loc.Id })
                .ToDictionary(x => x.Index, x => x.Id);

            _logger.LogDebug("Found {availablelocations} customized locations for modify", allLocationsForPrompt.Count);

            var currentTripPlan = _mapper.Map<TripPlanResponse>(trip);
            var firstDay = currentTripPlan.Days.FirstOrDefault();
            var firstLocation = firstDay?.Locations?.FirstOrDefault();
            _logger.LogDebug("Map from trip to trip plan response with data :\n" +
                            "Title: {title}\n" +
                            "Description: {description}\n" +
                            "Start: {start}\n" +
                            "End: {end}\n" +
                            "Size: {size}\n" +
                            "DayIndex1: {dayindex}\n" +
                            "DateIndex1: {date}\n" +
                            "TitleIndex1: {titleDay}\n" +
                            "LocationIdIndex1.1: {LocationId}\n" +
                            "LocationNameIndex1.1: {LocationName}",
                            currentTripPlan.Title,
                            currentTripPlan.Description,
                            currentTripPlan.Start,
                            currentTripPlan.End,
                            currentTripPlan.Size,
                            firstDay?.DayIndex,
                            firstDay?.Date,
                            firstDay?.Title,
                            firstLocation?.LocationId,
                            firstLocation?.Name);

            yield return Result<StreamEvent>.Success(new StreamEvent
            {
                Type = StreamEventType.Parsing,
                Data = new { message = "Đang điều chỉnh lịch trình..." }
            });

            var completeJsonBuffer = new StringBuilder();
            int totalPromptTokens = 0, totalOutputTokens = 0, responseTimeMs = 0;
            string providerName = "";

            await foreach (var chunk in _aiService.ModifyTripPlanStreamAsync(
                currentTripPlan, recentMessages, allLocationsForPrompt, request.UserRequest, personalization, constraints, cancellationToken))
            {
                if (chunk.IsFailure)
                {
                    yield return Result<StreamEvent>.Failure(chunk.Error!);
                    yield break;
                }

                var chunkData = chunk.Value!;
                completeJsonBuffer.Append(chunkData.Content);

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
            _logger.LogDebug("Finish call stream api with {prompt} prompt token and {output} output tokens", totalPromptTokens, totalOutputTokens);

            var parseResult = await _parser.ParseModifyTripResponse(completeJsonBuffer.ToString(), locationMap);
            if (parseResult.IsFailure)
            {
                yield return Result<StreamEvent>.Failure(parseResult.Error);
                yield break;
            }

            var subscription = await _userSubscriptionRepository.GetUserActiveSubscription(userId, cancellationToken);
            var cost = _usageTracker.CalculateCost(totalPromptTokens, totalOutputTokens, providerName);
            _logger.LogDebug("Total cost {cost}", cost);

            await _usageTracker.LogUsageAsync(userId, subscription?.Id, providerName,
                totalPromptTokens, totalOutputTokens, responseTimeMs, cost, 200, null, cancellationToken);

            yield return Result<StreamEvent>.Success(new StreamEvent
            {
                Type = StreamEventType.Complete,
                Data = parseResult.Value
            });

            yield return Result<StreamEvent>.Success(new StreamEvent
            {
                Type = StreamEventType.Saving,
                Data = new { message = "Đang lưu thay đổi..." }
            });

            var applyResult = await _mediator.Send(new ApplyTripModificationCommand
            {
                TripId = request.TripId,
                Modification = parseResult.Value!,
                UserRequest = request.UserRequest
            }, cancellationToken);

            if (applyResult.IsFailure)
            {
                // Save AI error message to DB so it persists after reload
                // NOTE: User message is managed by the parent handler (StreamAIChatCommandHandler).
                // It is either saved there (AI Mode) or was already saved via SignalR (Group Mode).
                var errorMsg = ChatMessage.Create(
                    tripId: request.TripId,
                    senderId: userId,
                    content: "Xin lỗi, mình không thể thực hiện thay đổi lúc này. Vui lòng thử lại.",
                    messageType: "text",
                    isAiMessage: true);
                await _chatMessageRepository.AddAsync(errorMsg);

                try
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not save error ChatMessages to DB due to a DB context failure (best effort).");
                }

                yield return Result<StreamEvent>.Failure(applyResult.Error);
                yield break;
            }

            yield return Result<StreamEvent>.Success(new StreamEvent
            {
                Type = StreamEventType.Saved,
                Data = applyResult.Value
            });
        }
    }
}
