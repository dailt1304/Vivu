using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Commands.UpdateMemberRole
{
    public class UpdateMemberRoleCommandHandler : IRequestHandler<UpdateMemberRoleCommand, Result<TripMemberDto>>
    {
        private readonly ITripMemberRepository _tripMemberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<UpdateMemberRoleCommandHandler> _logger;
        private readonly IMapper _mapper;

        public UpdateMemberRoleCommandHandler(
            ITripMemberRepository tripMemberRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            ILogger<UpdateMemberRoleCommandHandler> logger,
            IMapper mapper)
        {
            _tripMemberRepository = tripMemberRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<Result<TripMemberDto>> Handle(UpdateMemberRoleCommand request, CancellationToken cancellationToken)
        {
            // Auth
            if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var currentUserId))
            {
                _logger.LogWarning("Update Member Role failed: user not authenticated/invalid id. TraceId: {TraceId}", _currentUser.TraceId);
                return Result<TripMemberDto>.Failure(DomainErrors.Auth.InvalidToken);
            }

            _logger.LogInformation("Update Member Role. UserId: {UserId}, TripId: {TripId}", currentUserId, request.TripId);

            // Owner check
            var ownerId = await _tripMemberRepository.GetOwnerIdByTripId(request.TripId);
            if (ownerId == null)
            {
                _logger.LogWarning("Update Member Role failed: Trip owner not found. TripId: {TripId}", request.TripId);
                return Result<TripMemberDto>.Failure(DomainErrors.Trip.NotFoundById(request.TripId));
            }

            if (ownerId.Value != currentUserId)
            {
                _logger.LogWarning("Update Member Role denied: AccessDenied. TripId: {TripId}, UserId: {UserId}, OwnerId: {OwnerId}",
                    request.TripId, currentUserId, ownerId.Value);

                return Result<TripMemberDto>.Failure(DomainErrors.Trip.AccessDenied);
            }

            // Prevent assign owner role
            if (request.NewRole == TripRole.Owner)
            {
                return Result<TripMemberDto>.Failure(DomainErrors.TripMember.InvalidRoleChange);
            }

             // Member existence check - include User and UserProfile for mapping
             var member = await _tripMemberRepository.GetByTripAndUserAsync(request.TripId, request.MemberUserId, cancellationToken);

            if (member == null)
            {
                _logger.LogWarning("Update Member Role failed: Member not found. TripId: {TripId}, MemberUserId: {MemberUserId}",
                    request.TripId, request.MemberUserId);
                return Result<TripMemberDto>.Failure(DomainErrors.TripMember.NotFound);
            }

            // Update role
            member.Role = request.NewRole.ToString().ToLower();
            _tripMemberRepository.Update(member);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Update Member Role succeeded. TripId: {TripId}, MemberUserId: {MemberUserId}, NewRole: {NewRole}",
                request.TripId, request.MemberUserId, request.NewRole);

            var memberDto = _mapper.Map<TripMemberDto>(member);
            return Result<TripMemberDto>.Success(memberDto);
        }
    }
}