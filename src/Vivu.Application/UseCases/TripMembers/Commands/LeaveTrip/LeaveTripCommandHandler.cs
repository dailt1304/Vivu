using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Trips;
using Vivu.Application.UseCases.Notifications.Events;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripMembers.Commands.LeaveTrip
{
    public class LeaveTripCommandHandler : IRequestHandler<LeaveTripCommand, Result<bool>>
    {
        private readonly ITripRepository _tripRepository;
        private readonly ITripMemberRepository _tripMemberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly IUserRepository _userRepository;
        private readonly IPublisher _publisher;
        private readonly ITripHubService _tripHubService;
        private readonly ILogger<LeaveTripCommandHandler> _logger;

        public LeaveTripCommandHandler(
            ITripRepository tripRepository,
            ITripMemberRepository tripMemberRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            IUserRepository userRepository,
            IPublisher publisher,
            ITripHubService tripHubService,
            ILogger<LeaveTripCommandHandler> logger)
        {
            _tripRepository = tripRepository;
            _tripMemberRepository = tripMemberRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _userRepository = userRepository;
            _publisher = publisher;
            _tripHubService = tripHubService;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(LeaveTripCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Leave trip attempt. TripId: {TripId}", request.TripId);

            if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
            {
                _logger.LogWarning("Leave trip failed: Invalid or missing user ID");
                return Result<bool>.Failure(DomainErrors.Auth.InvalidToken);
            }

            _logger.LogDebug("User authenticated. UserId: {UserId}", userId);

            var trip = await _tripRepository.GetByIdAsync(request.TripId);
            if (trip == null)
            {
                _logger.LogWarning("Leave trip failed: Trip not found. TripId: {TripId}", request.TripId);
                return Result<bool>.Failure(DomainErrors.Trip.NotFoundById(request.TripId));
            }

            if (trip.IsDeleted)
            {
                _logger.LogWarning("Leave trip failed: Trip is deleted. TripId: {TripId}", request.TripId);
                return Result<bool>.Failure(DomainErrors.Trip.TripDeleted(request.TripId));
            }

            if (trip.UserId == userId)
            {
                _logger.LogWarning("Leave trip failed: Owner cannot leave trip. UserId: {UserId}, TripId: {TripId}", userId, request.TripId);
                return Result<bool>.Failure(DomainErrors.TripMember.OwnerCannotLeave);
            }

            var tripMember = await _tripMemberRepository.GetByTripAndUserAsync(request.TripId, userId, cancellationToken);
            if (tripMember == null)
            {
                _logger.LogWarning("Leave trip failed: User is not a member. UserId: {UserId}, TripId: {TripId}", userId, request.TripId);
                return Result<bool>.Failure(DomainErrors.TripMember.NotFound);
            }

            _tripMemberRepository.Remove(tripMember);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Broadcast real-time member change to trip group via SignalR
            var user = await _userRepository.GetByIdAsync(userId);
            await _tripHubService.BroadcastMemberChangedAsync(
                request.TripId, "left", userId, user?.UserProfile?.FullName);

            // Notify trip owner
            await _publisher.Publish(new MemberLeftTripEvent(
                tripOwnerId: trip.UserId,
                tripId: trip.Id,
                memberName: user?.UserProfile?.FullName ?? "Người dùng",
                tripTitle: trip.Title ?? "chuyến đi"), cancellationToken);

            _logger.LogInformation("User left trip successfully. UserId: {UserId}, TripId: {TripId}", userId, request.TripId);

            return Result<bool>.Success(true);
        }
    }
}
