using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.TripLocation;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Trips;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripLocation.Commands.UpdateTripLocation
{
    public class UpdateTripLocationHandler : IRequestHandler<UpdateTripLocationCommand, Result<TripLocationResponse>>
    {
        private readonly ITripDayRepository _tripDayRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly ITripLocationRepository _tripLocationRepository;
        private readonly ITripMemberRepository _tripMemberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateTripLocationHandler> _logger;
        private readonly ICurrentUser _currentUser;
        private readonly ITripHubService _tripHubService;

        public UpdateTripLocationHandler(
            ITripDayRepository tripDayRepository,
            ILocationRepository locationRepository,
            ITripLocationRepository tripLocationRepository,
            ITripMemberRepository tripMemberRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<UpdateTripLocationHandler> logger,
            ICurrentUser currentUser,
            ITripHubService tripHubService)
        {
            _tripDayRepository = tripDayRepository;
            _locationRepository = locationRepository;
            _tripLocationRepository = tripLocationRepository;
            _tripMemberRepository = tripMemberRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _currentUser = currentUser;
            _tripHubService = tripHubService;
        }

        public async Task<Result<TripLocationResponse>> Handle(UpdateTripLocationCommand request, CancellationToken cancellationToken)
        {
            // Check user authentication
            if (string.IsNullOrEmpty(_currentUser.Id))
            {
                _logger.LogWarning("Update location failed: User not authenticated");
                return Result<TripLocationResponse>.Failure(DomainErrors.Auth.InvalidToken);
            }

            var currentUserId = Guid.Parse(_currentUser.Id);
            _logger.LogInformation(
                "Updating trip location. UserId: {UserId}, TripLocationId: {TripLocationId}",
                currentUserId,
                request.TripLocationId);

            // Get existing TripLocation
            var tripLocation = await _tripLocationRepository.GetByIdAsync(request.TripLocationId);
            if (tripLocation == null)
            {
                _logger.LogWarning(
                    "Update location failed: TripLocation not found. TripLocationId: {TripLocationId}",
                    request.TripLocationId);
                return Result<TripLocationResponse>.Failure(new Error(
                    "TripLocation.NotFound",
                    $"Trip location with ID '{request.TripLocationId}' was not found."));
            }
            _logger.LogDebug("TripLocation found. TripLocationId: {TripLocationId}", tripLocation.Id);

            // Verify new TripDay exists
            var tripDay = await _tripDayRepository.GetByIdAsync(request.TripDayId);
            if (tripDay == null)
            {
                _logger.LogWarning(
                    "Update location failed: TripDay not found. TripDayId: {TripDayId}",
                    request.TripDayId);
                return Result<TripLocationResponse>.Failure(DomainErrors.Trip.NotFoundByTripDay(request.TripDayId));
            }
            _logger.LogDebug("TripDay found. TripDayId: {TripDayId}, TripId: {TripId}", tripDay.Id, tripDay.TripId);

            // Check if user has access to this trip (owner or editor)
            var member = await _tripMemberRepository.GetByTripAndUserAsync(tripDay.TripId, currentUserId, cancellationToken);
            if (member == null || (member.Role != "owner" && member.Role != "editor"))
            {
                _logger.LogWarning(
                    "Update location failed: Access denied. TripId: {TripId}, UserId: {UserId}, Role: {Role}",
                    tripDay.TripId,
                    currentUserId,
                    member?.Role ?? "none");
                return Result<TripLocationResponse>.Failure(DomainErrors.Trip.AccessDenied);
            }
            _logger.LogDebug("Access verified. UserId: {UserId} is {Role} of TripId: {TripId}", currentUserId, member.Role, tripDay.TripId);
            // Verify new Location exists
            var location = await _locationRepository.GetByIdAsync(request.LocationId);
            if (location == null)
            {
                _logger.LogWarning(
                    "Update location failed: Location not found. LocationId: {LocationId}",
                    request.LocationId);
                return Result<TripLocationResponse>.Failure(DomainErrors.Location.NotFoundById(request.LocationId));
            }
            _logger.LogDebug(
                "Location found. LocationId: {LocationId}, LocationName: {LocationName}",
                location.Id,
                location.Name);

            // Check time conflict (exclude current TripLocation)
            if (request.StartTime.HasValue && request.EndTime.HasValue)
            {
                var hasConflict = await _tripLocationRepository.ExistsTimeConflictAsync(
                    request.TripDayId,
                    request.LocationId,
                    request.StartTime.Value,
                    request.EndTime.Value,
                    excludeTripLocationId: request.TripLocationId);

                if (hasConflict)
                {
                    _logger.LogWarning(
                        "Update location failed: Time conflict. TripDayId: {TripDayId}, LocationId: {LocationId}, StartTime: {StartTime}, EndTime: {EndTime}",
                        request.TripDayId,
                        request.LocationId,
                        request.StartTime.Value,
                        request.EndTime.Value);
                    return Result<TripLocationResponse>.Failure(DomainErrors.TripLocation.TimeConflict);
                }
            }
            var oldTripDayId = tripLocation.TripDayId;
            var isMovingDay = request.TripDayId != oldTripDayId;
            // Update TripLocation
            tripLocation.Update(
                tripDayId: request.TripDayId,
                locationId: request.LocationId,
                orderIndex: request.OrderIndex,
                startTime: request.StartTime,
                endTime: request.EndTime,
                note: request.Note,
                transportMode: request.TransportMode
            );

            _logger.LogDebug(
                "Updating TripLocation. TripLocationId: {TripLocationId}, NewOrderIndex: {OrderIndex}, MovingDay: {MovingDay}",
                tripLocation.Id,
                tripLocation.OrderIndex,
                isMovingDay);

            // Clear navigation properties to prevent EF from tracking old objects (crucial for ForeignKey updates)
            tripLocation.TripDay = null!;
            tripLocation.Location = null!;

            _tripLocationRepository.Update(tripLocation);

            // Reorder NEW/Current Day
            var targetDayLocations = await _tripLocationRepository.GetTripLocationsByTripDayIdAsync(request.TripDayId);
            if (isMovingDay)
            {
                // If moving from another day, it won't be in the target list yet
                if (!targetDayLocations.Any(l => l.Id == tripLocation.Id))
                {
                    targetDayLocations.Add(tripLocation);
                }
            }
            _tripLocationRepository.ReorderTripLocationsAsync(targetDayLocations);

            // Reorder OLD Day (if moving)
            if (isMovingDay)
            {
                var sourceDayLocations = await _tripLocationRepository.GetTripLocationsByTripDayIdAsync(oldTripDayId);
                var itemInSource = sourceDayLocations.FirstOrDefault(l => l.Id == tripLocation.Id);
                if (itemInSource != null)
                {
                    sourceDayLocations.Remove(itemInSource);
                    _tripLocationRepository.ReorderTripLocationsAsync(sourceDayLocations);
                }
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("TripLocation updated in database. TripLocationId: {TripLocationId}", tripLocation.Id);

            // Load lại từ database với navigation property
            var updatedTripLocation = await _tripLocationRepository.GetByIdWithDetailsAsync(tripLocation.Id);
            var response = _mapper.Map<TripLocationResponse>(updatedTripLocation);

            _logger.LogInformation(
                "Trip location updated successfully. TripLocationId: {TripLocationId}, TripDayId: {TripDayId}, LocationId: {LocationId}, LocationName: {LocationName}",
                response.Id,
                response.TripDayId,
                response.LocationId,
                response.LocationName);

            // Broadcast real-time update to all trip members
            await _tripHubService.BroadcastItineraryChangedAsync(
                tripDay.TripId, "location_updated", currentUserId);

            return Result<TripLocationResponse>.Success(response);
        }
    }
}