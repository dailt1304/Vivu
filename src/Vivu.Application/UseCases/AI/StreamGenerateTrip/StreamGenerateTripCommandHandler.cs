using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.Interfaces.AI;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Trips;
using Vivu.Application.UseCases.AI.CreateTrip;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.AI.StreamGenerateTrip
{
    public class StreamGenerateTripCommandHandler
   : IStreamRequestHandler<StreamGenerateTripCommand, Result<StreamEvent>>
    {
        private readonly IStreamingAIService _aiService;
        private readonly IAIParsing _parser;
        private readonly IPromptBuilder _promptBuilder;
        private readonly ICityRepository _cityRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly ILocationCategoryRepository _locationCategoryRepository;
        private readonly ILocationZoneService _zoneService;
        private readonly IUserPersonalizationService _personalizationService;
        private readonly ITripItineraryOptimizer _optimizer;
        private readonly IMediator _mediator;
        private readonly IRateLimitService _rateLimitService;
        private readonly ITripLimitChecker _tripLimitChecker;
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;
        private readonly IUsageTrackingService _usageTracker;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<StreamGenerateTripCommandHandler> _logger;

        public StreamGenerateTripCommandHandler(
            IStreamingAIService aiService,
            IAIParsing parser,
            IPromptBuilder promptBuilder,
            IRateLimitService rateLimitService,
            ITripLimitChecker tripLimitChecker,
            ILocationCategoryRepository locationCategoryRepository,
            ICityRepository cityRepository,
            ILocationRepository locationRepository,
            ILocationZoneService zoneService,
            ITripItineraryOptimizer optimizer,
            IMediator mediator,
            ICurrentUser currentUser,
            ILogger<StreamGenerateTripCommandHandler> logger,
            IUserSubscriptionRepository userSubscriptionRepository,
            IUserPersonalizationService personalizationService,
            IUsageTrackingService usageTracker)
        {
            _tripLimitChecker = tripLimitChecker;
            _aiService = aiService;
            _rateLimitService = rateLimitService;
            _parser = parser;
            _promptBuilder = promptBuilder;
            _cityRepository = cityRepository;
            _locationRepository = locationRepository;
            _locationCategoryRepository = locationCategoryRepository;
            _zoneService = zoneService;
            _optimizer = optimizer;
            _userSubscriptionRepository = userSubscriptionRepository;
            _personalizationService = personalizationService;
            _usageTracker = usageTracker;
            _mediator = mediator;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async IAsyncEnumerable<Result<StreamEvent>> Handle(
          StreamGenerateTripCommand request,
          [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
            {
                yield return Result<StreamEvent>.Failure(DomainErrors.Auth.InvalidToken);
                yield break;
            }

            var canCreateTrip = await _tripLimitChecker.CanCreateTripAsync(userId, cancellationToken);
            if (!canCreateTrip)
            {
                _logger.LogWarning("Create trip failed: Trip limit reached. UserId: {UserId}", userId);
                yield return Result<StreamEvent>.Failure(DomainErrors.Trip.TripLimitReached);
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
                Data = new { message = "Starting trip generation..." }
            });

            var city = await _cityRepository.GetByIdAsync(request.CityId);
            if (city == null)
            {
                yield return Result<StreamEvent>.Failure(DomainErrors.Cities.NotFound);
                yield break;
            }

            var daysCount = (request.EndDate.ToDateTime(TimeOnly.MinValue) -
                            request.StartDate.ToDateTime(TimeOnly.MinValue)).Days;

            var allLocations = await _locationRepository.GetAllLocationByCity(request.CityId);

            TripConstraints? constraints = null;
            if (!string.IsNullOrWhiteSpace(request.Notes))
            {
                yield return Result<StreamEvent>.Success(new StreamEvent
                {
                    Type = StreamEventType.Parsing,
                    Data = new { message = "Đang phân tích yêu cầu..." }
                });
                var locationcategories = await _locationCategoryRepository.GetAllCategoryNamesAsync();

                constraints = await _parser.ParseNotesConstraintsAsync(request.Notes, locationcategories, cancellationToken);
                if (constraints == null || constraints.Equals(new TripConstraints())) {
                    _logger.LogWarning("Parsed error with NOTE {notes}", request.Notes);
                }
                _logger.LogDebug("Parsed constraints: {@Constraints}", constraints);
            }

            var personalization = await _personalizationService.BuildContextAsync(
                userId,
                request.GroupSize > 0 ? request.GroupSize : 1,
                request.GroupComposition,
                cancellationToken);

            var tripPlanDto = new TripPlanDto
            {
                UserId = userId,
                CityId = request.CityId,
                Destination = city.Name,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                GroupSize = request.GroupSize > 0 ? request.GroupSize : 1,
                Preferences = request.Preferences,
                Budget = request.Budget,
                Notes = request.Notes,
                Title = request.Title,
                Personalization = personalization
            };
            
            var selectedLocations = _zoneService.SelectLocationsForTrip(
                allLocations,
                daysCount,
                request.Preferences,
                constraints,
                personalization);

            var locationMap = selectedLocations
                .Select((loc, idx) => new { Index = idx + 1, loc.Id })
                .ToDictionary(x => x.Index.ToString(), x => x.Id.ToString());

            yield return Result<StreamEvent>.Success(new StreamEvent
            {
                Type = StreamEventType.LocationMap,
                Data = locationMap
            });

            int totalPromptTokens = 0;
            int totalOutputTokens = 0;
            string providername = "";
            int responseTimeMs = 0;

            var completeJsonBuffer = new StringBuilder();

            await foreach (var chunk in _aiService.GenerateTripPlanStreamAsync(
                                          tripPlanDto,
                                          selectedLocations,
                                          cancellationToken))
            {
                if (chunk.IsFailure)
                {
                    yield return Result<StreamEvent>.Failure(chunk.Error!);
                    yield break;
                }

                var chunkData = chunk.Value!;
                completeJsonBuffer.Append(chunkData.Content);
                if (chunkData.PromptTokens.HasValue)
                    totalPromptTokens = chunkData.PromptTokens.Value;
                if (chunkData.CompletionTokens.HasValue)
                    totalOutputTokens = chunkData.CompletionTokens.Value;
                if (!string.IsNullOrEmpty(chunkData.ProviderName))
                    providername = chunkData.ProviderName;
                if (chunkData.ResponseTimeMs.HasValue)
                    responseTimeMs = chunkData.ResponseTimeMs.Value;

                yield return Result<StreamEvent>.Success(new StreamEvent
                {
                    Type = StreamEventType.Chunk,
                    Data = chunkData.Content
                });
            }

            var completeJson = completeJsonBuffer.ToString();

            var parseResult = await _parser.ParseTripPlanResponse(completeJson, selectedLocations);
            if (parseResult.IsFailure)
            {
                yield return Result<StreamEvent>.Failure(parseResult.Error);
                yield break;
            }

            // Post-process: Nearest Neighbor reorder (Option C)
            var tripPlan = _optimizer.Optimize(parseResult.Value!, selectedLocations);

            var subscription = await _userSubscriptionRepository.GetUserActiveSubscription(userId, cancellationToken);

            var cost = _usageTracker.CalculateCost(totalPromptTokens, totalOutputTokens, providername);
            await _usageTracker.LogUsageAsync(
                userId,
                subscription?.Id,
                providername,
                totalPromptTokens,
                totalOutputTokens,
                responseTimeMs,
                cost,
                200,
                null,
                cancellationToken);

            yield return Result<StreamEvent>.Success(new StreamEvent
            {
                Type = StreamEventType.Complete,
                Data = tripPlan
            });

            if (request.AutoSave)
            {
                yield return Result<StreamEvent>.Success(new StreamEvent
                {
                    Type = StreamEventType.Saving,
                    Data = new { message = "Saving your trip..." }
                });

                var userPromptSummary = _promptBuilder.BuildUserPromptFromForm(request, city.Name);

                var createCommand = new AICreateTripCommand
                {
                    TripPlan = tripPlan,
                    UserPrompt = userPromptSummary,
                    cityId = request.CityId,
                    CoverUrl = request.CoverUrl ?? city.Image,
                    GenerateInviteCode = request.GenerateInviteCode,
                    PersonalizationContextJson = personalization != null 
                        ? JsonSerializer.Serialize(personalization) 
                        : null,
                    ConstraintsJson = constraints != null
                        ? JsonSerializer.Serialize(constraints)
                        : null
                };

                var saveResult = await _mediator.Send(createCommand, cancellationToken);

                if (saveResult.IsFailure)
                {
                    yield return Result<StreamEvent>.Failure(saveResult.Error);
                    yield break;
                }

                yield return Result<StreamEvent>.Success(new StreamEvent
                {
                    Type = StreamEventType.Saved,
                    Data = saveResult.Value
                });
            }
        }
    }
}
