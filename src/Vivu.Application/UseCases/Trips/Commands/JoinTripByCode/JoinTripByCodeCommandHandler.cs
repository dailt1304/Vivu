using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Trips;
using Vivu.Application.UseCases.Notifications.Events;
using Vivu.Application.UseCases.Trips.Commands.CreateTrip;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Commands.JoinTripByCode
{
    public class JoinTripByCodeCommandHandler : IRequestHandler<JoinTripByCodeCommand, Result<TripMemberDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITripRepository _tripRepository;
        private readonly ITripMemberRepository _memberRepository;
        private readonly ICurrentUser _currentUserService;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IPublisher _publisher;
        private readonly ITripHubService _tripHubService;
        private readonly ILogger<JoinTripByCodeCommandHandler> _logger;

        public JoinTripByCodeCommandHandler(IUnitOfWork unitOfWork, ICurrentUser currentUserService, 
                            ITripRepository tripRepository, ITripMemberRepository memberRepository, IUserRepository userRepository,
                            IMapper mapper, IPublisher publisher, ITripHubService tripHubService, ILogger<JoinTripByCodeCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _tripRepository = tripRepository;
            _memberRepository = memberRepository;
            _mapper = mapper;
            _publisher = publisher;
            _tripHubService = tripHubService;
            _logger = logger;
            _userRepository = userRepository;
            _memberRepository = memberRepository;
        }

        public async Task<Result<TripMemberDto>> Handle(JoinTripByCodeCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_currentUserService.Id) || !Guid.TryParse(_currentUserService.Id, out var userId))
            {
                _logger.LogWarning("Join trip failed: Invalid or missing user ID");
                return Result<TripMemberDto>.Failure(DomainErrors.Auth.InvalidToken);
            }
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return Result<TripMemberDto>.Failure(DomainErrors.User.NotFound);
            }
            var trip = await _tripRepository
                .GetByInviteCodeAsync(request.InviteCode, cancellationToken);

            if (trip == null)
                return Result<TripMemberDto>.Failure(DomainErrors.Trip.InvalidInviteCode);

            if (trip.IsDeleted)
                return Result<TripMemberDto>.Failure(DomainErrors.Trip.TripDeleted(trip.Id));

            var existingMember = await _memberRepository
                .GetByTripAndUserAsync(trip.Id, userId, cancellationToken);

            if (existingMember != null)
                return Result<TripMemberDto>.Failure(DomainErrors.Trip.AlreadyMember);


            var tripMember = Domain.Entities.TripMember.Create(
                tripId: trip.Id,
                userId: userId,
                ownerId: trip.UserId,
                role: "viewer");

            await _memberRepository.AddAsync(tripMember);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Notify trip owner that a new member joined
            await _publisher.Publish(new MemberJoinedTripEvent(
                tripOwnerId: trip.UserId,
                tripId: trip.Id,
                memberName: user.UserProfile?.FullName ?? "Người dùng",
                tripTitle: trip.Title ?? "chuyến đi"), cancellationToken);

            // Broadcast real-time member change to trip group via SignalR
            await _tripHubService.BroadcastMemberChangedAsync(
                trip.Id, "joined", userId, user.UserProfile?.FullName);

            tripMember.User = user;
            tripMember.Trip = trip;
            var tripdto = _mapper.Map<TripMemberDto>(tripMember);

            return Result<TripMemberDto>.Success(tripdto);
        }
    }
}
