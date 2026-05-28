using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Auth;
using Vivu.Application.DTOs.Responses.TripDay;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Trips;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Vivu.Application.UseCases.Notifications.Events;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripMember.Command.RemoveMemberFromTrip
{
    public class RemoveMemberFromTripCommandHandler : IRequestHandler<RemoveMemberFromTripCommand, Result<string>>
    {
        private readonly ICurrentUser _currentUser;
        private readonly ITripRepository _tripRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITripMemberRepository _memberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublisher _publisher;
        private readonly ITripHubService _tripHubService;
        private readonly ILogger<RemoveMemberFromTripCommandHandler> _logger;

        public RemoveMemberFromTripCommandHandler(ICurrentUser currentUser, ITripRepository tripRepository, 
            IUserRepository userRepository, ITripMemberRepository memberRepository,
            IUnitOfWork unitOfWork, IPublisher publisher, ITripHubService tripHubService, ILogger<RemoveMemberFromTripCommandHandler> logger)
        {
            _currentUser = currentUser;
            _tripRepository = tripRepository;
            _userRepository = userRepository;
            _memberRepository = memberRepository;
            _unitOfWork = unitOfWork;
            _publisher = publisher;
            _tripHubService = tripHubService;
            _logger = logger;
        }

        public async Task<Result<string>> Handle(RemoveMemberFromTripCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var currentUserId))
            {
                _logger.LogWarning("Remove member failed: user not authenticated/invalid id. TraceId: {TraceId}", _currentUser.TraceId);
                return Result<string>.Failure(DomainErrors.Auth.InvalidToken);
            }
            var trip = await _tripRepository.GetByIdAsync(request.TripId);
            if (trip == null)
            {
                _logger.LogWarning("Remove member failed: trip not found");
                return Result<string>.Failure(DomainErrors.Trip.NotFound);
            }
            if (trip.IsDeleted)
            {
                _logger.LogWarning("Remove member failed: trip was deleteed");
                return Result<string>.Failure(DomainErrors.Trip.TripDeleted(trip.Id));
            }
            var owner = await _memberRepository.GetOwnerIdByTripId(request.TripId);
            if (owner == null)
            {
                _logger.LogWarning("Remove member failed: owner not found in this trip");
                return Result<string>.Failure(DomainErrors.TripMember.NotFound);
            }

            if (owner != currentUserId)
            {
                _logger.LogWarning("Remove member failed: only owner can remove member from trip");
                return Result<string>.Failure(DomainErrors.TripMember.NotOwner);
            }

            if (owner == request.UserId)
            {
                _logger.LogWarning("Remove member failed: cannot remove yourself");
                return Result<string>.Failure(DomainErrors.TripMember.CannotRemoveSelf);
            }

            var member = await _userRepository.GetByIdAsync(request.UserId);
            if (member == null)
            {
                _logger.LogWarning("Remove member failed: member not found");
                return Result<string>.Failure(DomainErrors.User.NotFoundById(request.UserId));
            }
            var tripmember = await _memberRepository.GetByTripAndUserAsync(trip.Id, member.Id, cancellationToken);
            if (tripmember == null)
            {
                _logger.LogWarning("Remove member failed: member not found");
                return Result<string>.Failure(DomainErrors.TripMember.NotFoundMember);
            }
            _memberRepository.Remove(tripmember);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Broadcast real-time member change to remaining members
            await _tripHubService.BroadcastMemberChangedAsync(
                trip.Id, "removed", request.UserId, member.UserProfile?.FullName);

            // Force-kick the removed user out of trip page
            await _tripHubService.NotifyMemberKickedAsync(
                trip.Id, request.UserId, trip.Title ?? "chuyến đi");

            // Notify the removed member via notification bell
            await _publisher.Publish(new MemberRemovedFromTripEvent(
                removedUserId: request.UserId,
                tripId: trip.Id,
                tripTitle: trip.Title ?? "chuyến đi"), cancellationToken);

            return Result<string>.Success($"Remove member {member.UserProfile.FullName} from trip {trip.Title}");
        }
    }
}
