using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.TripLocation;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Trips;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripLocation.Commands.RemoveTripLocation
{
    public class RemoveTripLocationHandler : IRequestHandler<RemoveTripLocationCommand, Result<TripLocationResponse>>
    {
        private readonly ITripDayRepository _tripDayRepository;
        private readonly ITripLocationRepository _tripLocationRepository;
        private readonly ITripMemberRepository _tripMemberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<RemoveTripLocationHandler> _logger;
        private readonly ICurrentUser _currentUser;
        private readonly ITripHubService _tripHubService;

        public RemoveTripLocationHandler(
            ITripDayRepository tripDayRepository,
            ITripLocationRepository tripLocationRepository,
            ITripMemberRepository tripMemberRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<RemoveTripLocationHandler> logger,
            ICurrentUser currentUser,
            ITripHubService tripHubService)
        {
            _tripDayRepository = tripDayRepository;
            _tripLocationRepository = tripLocationRepository;
            _tripMemberRepository = tripMemberRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _currentUser = currentUser;
            _tripHubService = tripHubService;
        }

        public async Task<Result<TripLocationResponse>> Handle(RemoveTripLocationCommand request, CancellationToken cancellationToken)
        {
            // Check user authentication
            if (string.IsNullOrEmpty(_currentUser.Id))
            {
                _logger.LogWarning("Remove location failed: User not authenticated");
                return Result<TripLocationResponse>.Failure(DomainErrors.Auth.InvalidToken);
            }

            var currentUserId = Guid.Parse(_currentUser.Id);
            _logger.LogInformation(
                "Removing trip location. UserId: {UserId}, TripLocationId: {TripLocationId}",
                currentUserId,
                request.TripLocationId);

            // Get existing TripLocation
            var tripLocation = await _tripLocationRepository.GetByIdAsync(request.TripLocationId);
            if (tripLocation == null)
            {
                _logger.LogWarning(
                    "Remove location failed: TripLocation not found. TripLocationId: {TripLocationId}",
                    request.TripLocationId);
                return Result<TripLocationResponse>.Failure(new Error(
                    "TripLocation.NotFound",
                    $"Trip location with ID '{request.TripLocationId}' was not found."));
            }
            _logger.LogDebug("TripLocation found. TripLocationId: {TripLocationId}, TripDayId: {TripDayId}",
                tripLocation.Id, tripLocation.TripDayId);

            // TripDay is already included in tripLocation from GetByIdAsync (Include)
            // Don't load it again separately to avoid EF Core tracking conflicts
            var tripDay = tripLocation.TripDay;
            if (tripDay == null)
            {
                _logger.LogWarning(
                    "Remove location failed: TripDay not found. TripDayId: {TripDayId}",
                    tripLocation.TripDayId);
                return Result<TripLocationResponse>.Failure(DomainErrors.Trip.NotFoundByTripDay(tripLocation.TripDayId));
            }
            _logger.LogDebug("TripDay found. TripDayId: {TripDayId}, TripId: {TripId}", tripDay.Id, tripDay.TripId);

            // Check if user has access to this trip (owner or member)
            var member = await _tripMemberRepository.GetByTripAndUserAsync(tripDay.TripId, currentUserId, cancellationToken);
            if (member == null || (member.Role != "owner" && member.Role != "editor"))
            {
                _logger.LogWarning(
                    "Remove location failed: Access denied. TripId: {TripId}, UserId: {UserId}, Role: {Role}",
                    tripDay.TripId,
                    currentUserId,
                    member?.Role ?? "none");
                return Result<TripLocationResponse>.Failure(DomainErrors.Trip.AccessDenied);
            }

            // Map to response before deleting
            var response = _mapper.Map<TripLocationResponse>(tripLocation);

            // Remove the trip location
            _tripLocationRepository.Remove(tripLocation);
            // Reorder remaining locations in memory
            var remainingLocations = await _tripLocationRepository.GetTripLocationsByTripDayIdAsync(tripLocation.TripDayId);
            var itemToRemove = remainingLocations.FirstOrDefault(l => l.Id == tripLocation.Id);
            if (itemToRemove != null)
            {
                remainingLocations.Remove(itemToRemove);
            }
            _tripLocationRepository.ReorderTripLocationsAsync(remainingLocations);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Trip location removed successfully. TripLocationId: {TripLocationId}, UserId: {UserId}",
                request.TripLocationId,
                currentUserId);

            // Broadcast real-time update to all trip members
            await _tripHubService.BroadcastItineraryChangedAsync(
                tripDay.TripId, "location_removed", currentUserId);

            return Result<TripLocationResponse>.Success(response);
        }
    }
}