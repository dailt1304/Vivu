using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.AI;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Trips;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.AI.CreateTrip
{
    public class AICreateTripCommandHandler : IRequestHandler<AICreateTripCommand, Result<DetailedTripDto>>
    {
        private readonly ICurrentUser _currentUser;
        private readonly ILocationRepository _locationRepository;
        private readonly ITripRepository _tripRepository;
        private readonly IChatMessageRepository _chatMessageRepository;
        private readonly ITripMemberRepository _tripMemberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITripDayRepository _tripDayRepository;
        private readonly IMapper _mapper;
        private readonly IAIConvert _aiConvert;
        private readonly ITripLocationRepository _tripLocationRepository;
        private readonly ITripLocationAlternativeRepository _tripLocationAlternativeRepository;
        private readonly IInviteCodeGenerator _inviteCodeGenerator;
        private readonly ILogger<AICreateTripCommandHandler> _logger;

        public AICreateTripCommandHandler(
            ICurrentUser currentUser,
            IInviteCodeGenerator inviteCodeGenerator,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITripLocationRepository tripLocationRepository,
            ITripLocationAlternativeRepository tripLocationAlternativeRepository,
            IChatMessageRepository chatMessageRepository,
            IAIConvert aiConvert,
            ITripDayRepository tripDayRepository,
            ITripRepository tripRepository,
            ITripMemberRepository tripMemberRepository,
            ILocationRepository locationRepository,
            ILogger<AICreateTripCommandHandler> logger)
        {
            _currentUser = currentUser;
            _tripDayRepository = tripDayRepository;
            _unitOfWork = unitOfWork;
            _aiConvert = aiConvert;
            _mapper = mapper;
            _tripLocationRepository = tripLocationRepository;
            _tripLocationAlternativeRepository = tripLocationAlternativeRepository;
            _chatMessageRepository = chatMessageRepository;
            _locationRepository = locationRepository;
            _tripRepository = tripRepository;
            _tripMemberRepository = tripMemberRepository;
            _inviteCodeGenerator = inviteCodeGenerator;
            _logger = logger;
        }

        public async Task<Result<DetailedTripDto>> Handle(
            AICreateTripCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Starting CreateTripCommand by AI handler. TraceId: {TraceId}", _currentUser.TraceId);

            if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var currentUserId))
            {
                _logger.LogWarning("Create trip failed: user not authenticated/invalid id. TraceId: {TraceId}", _currentUser.TraceId);
                return Result<DetailedTripDto>.Failure(DomainErrors.Auth.InvalidToken);
            }

            var tripPlan = request.TripPlan;

            _logger.LogInformation(
                "Creating trip for user {UserId}: {Title}, {DaysCount} days, {LocationsCount} locations. TraceId: {TraceId}",
                currentUserId, tripPlan.Title, tripPlan.Days.Count,
                tripPlan.Days.Sum(d => d.Locations.Count), _currentUser.TraceId);

            // Collect ALL location IDs referenced in the plan: both primaries and alternatives
            var allLocationIds = tripPlan.Days
                .SelectMany(d => d.Locations)
                .SelectMany(l => l.Alternatives
                    .Select(a => a.LocationId)
                    .Prepend(l.LocationId))   // primary first, then alternatives
                .Distinct()
                .ToList();

            _logger.LogDebug("Validating {Count} unique location IDs", allLocationIds.Count);

            var existingLocationIds = await _locationRepository.ValidateAllLocationId(allLocationIds, cancellationToken);

            var missingLocationIds = allLocationIds.Except(existingLocationIds).ToList();

            if (missingLocationIds.Any())
            {
                _logger.LogWarning(
                    "Invalid location IDs found for user {UserId}: {LocationIds}. TraceId: {TraceId}",
                    currentUserId, string.Join(", ", missingLocationIds), _currentUser.TraceId);

                return Result<DetailedTripDto>.Failure(DomainErrors.Location.InvalidLocations(missingLocationIds));
            }

            _logger.LogDebug("All location IDs validated successfully");

            try
            {
                string? inviteCode = null;
                if (request.GenerateInviteCode)
                {
                    inviteCode = await _inviteCodeGenerator.Generate(cancellationToken);
                    _logger.LogDebug("Generated invite code: {InviteCode}", inviteCode);
                }

                var startDate = tripPlan.Start != DateOnly.MinValue
                    ? tripPlan.Start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                    : (DateTime?)null;

                var endDate = tripPlan.End != DateOnly.MinValue
                    ? tripPlan.End.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                    : (DateTime?)null;

                var trip = Trip.Create(
                    userId: currentUserId,
                    title: tripPlan.Title,
                    description: tripPlan.Description,
                    coverUrl: request.CoverUrl,
                    startDate: startDate,
                    endDate: endDate,
                    tripSize: tripPlan.Size > 0 ? tripPlan.Size : null,
                    isPublic: false,
                    cityId: request.cityId,
                    inviteCode: inviteCode
                );

                trip.PersonalizationContextJson = request.PersonalizationContextJson;
                trip.ConstraintsJson = request.ConstraintsJson;

                await _tripRepository.AddAsync(trip);
                _logger.LogInformation("Created trip entity: {TripId}", trip.Id);

                var tripMember = Domain.Entities.TripMember.Create(
                    tripId: trip.Id,
                    userId: currentUserId,
                    ownerId: currentUserId,
                    role: "owner");
                await _tripMemberRepository.AddAsync(tripMember);
                _logger.LogDebug("TripMember created for owner. TripId: {TripId}, UserId: {UserId}", trip.Id, currentUserId);

                var ideasDay = Domain.Entities.TripDay.Create(
                    TripId: trip.Id,
                    Tittle: "Ideas",
                    DayIndex: 0
                );
                await _tripDayRepository.AddAsync(ideasDay);
                _logger.LogDebug("Created 'Ideas' TripDay (DayIndex 0) for trip {TripId}", trip.Id);

                foreach (var dayPlan in tripPlan.Days.OrderBy(d => d.DayIndex))
                {
                    _logger.LogDebug(
                        "Processing day {DayIndex}: {Title} with {LocationsCount} locations",
                        dayPlan.DayIndex, dayPlan.Title, dayPlan.Locations.Count);

                    var dayDate = dayPlan.Date != DateOnly.MinValue
                        ? dayPlan.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                        : (DateTime?)null;

                    var tripDay = Domain.Entities.TripDay.Create(
                        TripId: trip.Id,
                        Tittle: dayPlan.Title,
                        DayDate: dayDate,
                        DayIndex: dayPlan.DayIndex
                    );

                    await _tripDayRepository.AddAsync(tripDay);
                    _logger.LogDebug("Created TripDay: {TripDayId} for day {DayIndex}", tripDay.Id, dayPlan.DayIndex);
                    foreach (var locationPlan in dayPlan.Locations.OrderBy(l => l.OrderIndex))
                    {
                        var startTime = _aiConvert.ConvertTimeOnlyToTimeSpan(locationPlan.StartTime);
                        var endTime = _aiConvert.ConvertTimeOnlyToTimeSpan(locationPlan.EndTime);

                        var tripLocation = Domain.Entities.TripLocation.Create(
                            tripDayId: tripDay.Id,
                            locationId: locationPlan.LocationId,
                            orderIndex: locationPlan.OrderIndex,
                            startTime: startTime,
                            endTime: endTime,
                            note: locationPlan.Description,
                            transportMode: locationPlan.TransportMode
                        );

                        await _tripLocationRepository.AddAsync(tripLocation);
                        _logger.LogDebug(
                            "Created TripLocation: {TripLocationId}, Location: {LocationId}, Order: {Order}",
                            tripLocation.Id, locationPlan.LocationId, locationPlan.OrderIndex);

                        // Persist alternative locations (up to MaxAlternatives = 2)
                        var altsToSave = locationPlan.Alternatives
                            .Take(Domain.Entities.TripLocation.MaxAlternatives)
                            .ToList();

                        for (var altIdx = 0; altIdx < altsToSave.Count; altIdx++)
                        {
                            var alt = altsToSave[altIdx];
                            var tripAlt = Domain.Entities.TripLocationAlternative.Create(
                                tripLocationId: tripLocation.Id,
                                locationId: alt.LocationId,
                                priority: altIdx + 1,   // 1-based priority
                                reason: alt.Reason
                            );
                            await _tripLocationAlternativeRepository.AddAsync(tripAlt);
                            _logger.LogDebug(
                                "Created TripLocationAlternative: {AltId}, Location: {LocationId}, Priority: {Priority}",
                                tripAlt.Id, alt.LocationId, tripAlt.Priority);
                        }
                    }
                }
                if (!string.IsNullOrWhiteSpace(request.UserPrompt))
                {
                    var userMessage = ChatMessage.Create(
                        tripId: trip.Id,
                        senderId: currentUserId,
                        content: request.UserPrompt,
                        messageType: "initial_prompt",
                        isAiMessage: false
                    );
                    await _chatMessageRepository.AddAsync(userMessage);
                }

                var aiContent = JsonSerializer.Serialize(request.TripPlan, new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                var aiMessage = ChatMessage.Create(
                    tripId: trip.Id,
                    senderId: currentUserId, 
                    content: aiContent,
                    messageType: "trip_plan",
                    isAiMessage: true
                );
                await _chatMessageRepository.AddAsync(aiMessage);

                _logger.LogDebug("Saving trip and related entities to database");
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Trip created successfully: TripId={TripId}, User={UserId}, Days={Days}, Locations={Locations}. TraceId: {TraceId}",
                    trip.Id, currentUserId, tripPlan.Days.Count,
                    tripPlan.Days.Sum(d => d.Locations.Count), _currentUser.TraceId);


                var tripresponse = await _tripRepository.GetTripByIdWithDetailsAsync(trip.Id, cancellationToken);
                var response = _mapper.Map<DetailedTripDto>(tripresponse);

                return Result<DetailedTripDto>.Success(response);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while creating trip for user {UserId}. TraceId: {TraceId}",
                    currentUserId, _currentUser.TraceId);

                return Result<DetailedTripDto>.Failure(DomainErrors.Trip.DatabaseError);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while creating trip for user {UserId}. TraceId: {TraceId}",
                    currentUserId, _currentUser.TraceId);

                return Result<DetailedTripDto>.Failure(DomainErrors.Trip.UnexpectedError);
            }
        }

    }
}