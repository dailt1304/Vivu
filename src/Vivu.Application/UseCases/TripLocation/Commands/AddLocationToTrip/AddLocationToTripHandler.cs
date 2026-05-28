using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.TripLocation;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Trips;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripLocation.Commands.AddLocationToTrip
{
    public class AddLocationToTripHandler : IRequestHandler<AddLocationToTripCommand, Result<TripLocationResponse>>
    {
        private readonly ITripDayRepository _tripDayRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly ITripLocationRepository _tripLocationRepository;
        private readonly ITripMemberRepository _tripMemberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AddLocationToTripHandler> _logger;
        private readonly ICurrentUser _currentUser;
        private readonly ITripHubService _tripHubService;

        public AddLocationToTripHandler(
            ITripDayRepository tripDayRepository,
            ILocationRepository locationRepository,
            ITripLocationRepository tripLocationRepository,
            ITripMemberRepository tripMemberRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<AddLocationToTripHandler> logger,
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

        public async Task<Result<TripLocationResponse>> Handle(AddLocationToTripCommand request, CancellationToken cancellationToken)
        {
            // Check user authentication
            if (string.IsNullOrEmpty(_currentUser.Id))
            {
                _logger.LogWarning("Add location failed: User not authenticated");
                return Result<TripLocationResponse>.Failure(DomainErrors.Auth.InvalidToken);
            }

            var currentUserId = Guid.Parse(_currentUser.Id);
            _logger.LogInformation(
                "Adding location to trip. UserId: {UserId}, TripDayId: {TripDayId}, LocationId: {LocationId}, OrderIndex: {OrderIndex}",
                currentUserId,
                request.TripDayId,
                request.LocationId,
                request.OrderIndex);


            // Verify TripDay exists
            var tripDay = await _tripDayRepository.GetByIdAsync(request.TripDayId);
            if (tripDay == null)
            {
                _logger.LogWarning(
                    "Add location failed: TripDay not found. TripDayId: {TripDayId}",
                    request.TripDayId);
                return Result<TripLocationResponse>.Failure(DomainErrors.Trip.NotFoundByTripDay(request.TripDayId));
            }
            _logger.LogDebug("TripDay found. TripDayId: {TripDayId}, TripId: {TripId}", tripDay.Id, tripDay.TripId);

            // Check if user has access to this trip (owner or member)
            var member = await _tripMemberRepository.GetByTripAndUserAsync(tripDay.TripId, currentUserId, cancellationToken);
            if (member == null || (member.Role != "owner" && member.Role != "editor"))
            {
                _logger.LogWarning(
                   "Add location failed: Access denied. TripId: {TripId}, UserId: {UserId}, Role: {Role}",
                    tripDay.TripId,
                    currentUserId,
                    member?.Role ?? "none");
                return Result<TripLocationResponse>.Failure(DomainErrors.Trip.AccessDenied);
            }
            _logger.LogDebug("Access verified. UserId: {UserId} is owner of TripId: {TripId}", currentUserId, tripDay.TripId);

            // Verify Location exists
            var location = await _locationRepository.GetByIdAsync(request.LocationId);
            if (location == null)
            {
                _logger.LogWarning(
                    "Add location failed: Location not found. LocationId: {LocationId}",
                    request.LocationId);
                return Result<TripLocationResponse>.Failure(DomainErrors.Location.NotFoundById(request.LocationId));
            }
            _logger.LogDebug(
                "Location found. LocationId: {LocationId}, LocationName: {LocationName}",
                location.Id,
                location.Name);
            
            // Check conflict when both startTime and endTime has value
            if (request.StartTime.HasValue && request.EndTime.HasValue)
            {
                var overlap = await _tripLocationRepository.ExistsTimeConflictAsync(request.TripDayId, request.LocationId, request.StartTime.Value, request.EndTime.Value);
                if (overlap)
                {
                    _logger.LogWarning(
                       "Add location failed: time conflict. TripDayId: {TripDayId}, LocationId: {LocationId}, Start: {Start}, End: {End}",
                       request.TripDayId, request.LocationId, request.StartTime.Value, request.EndTime.Value);
                    return Result<TripLocationResponse>.Failure(DomainErrors.TripLocation.TimeConflict);
                }
            }

            // Create TripLocation
            var tripLocation = Domain.Entities.TripLocation.Create(
                tripDayId: request.TripDayId,
                locationId: request.LocationId,
                orderIndex: request.OrderIndex,
                startTime: request.StartTime,
                endTime: request.EndTime,
                note: request.Note,
                transportMode: request.TransportMode
            );
            _logger.LogDebug(
                "Creating TripLocation. TripLocationId: {TripLocationId}, TransportMode: {TransportMode}",
                tripLocation.Id,
                tripLocation.TransportMode ?? "None");

            await _tripLocationRepository.AddAsync(tripLocation);
            var locations = await _tripLocationRepository.GetTripLocationsByTripDayIdAsync(request.TripDayId);
            locations.Add(tripLocation);
            _tripLocationRepository.ReorderTripLocationsAsync(locations);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("TripLocation saved to database. TripLocationId: {TripLocationId}", tripLocation.Id);

            // Load location for mapping
            tripLocation.Location = location;

            // Map to response using AutoMapper
            var response = _mapper.Map<TripLocationResponse>(tripLocation);

            _logger.LogInformation(
                "Location added to trip successfully. TripLocationId: {TripLocationId}, TripDayId: {TripDayId}, LocationId: {LocationId}, LocationName: {LocationName}",
                response.Id,
                response.TripDayId,
                response.LocationId,
                response.LocationName);

            // Broadcast real-time update to all trip members
            await _tripHubService.BroadcastItineraryChangedAsync(
                tripDay.TripId, "location_added", currentUserId);

            return Result<TripLocationResponse>.Success(response);
        }
    }
}
