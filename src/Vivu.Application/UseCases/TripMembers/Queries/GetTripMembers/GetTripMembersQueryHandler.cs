using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.Common.Extensions;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripMembers.Queries.GetTripMembers
{
    public class GetTripMembersQueryHandler : IRequestHandler<GetTripMembersQuery, Result<PaginatedList<TripMemberDto>>>
    {
        private readonly ITripRepository _tripRepository;
        private readonly ITripMemberRepository _tripMemberRepository;
        private readonly IMapper _mapper;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<GetTripMembersQueryHandler> _logger;

        public GetTripMembersQueryHandler(
            ITripRepository tripRepository,
            ITripMemberRepository tripMemberRepository,
            IMapper mapper,
            ICurrentUser currentUser,
            ILogger<GetTripMembersQueryHandler> logger)
        {
            _tripRepository = tripRepository;
            _tripMemberRepository = tripMemberRepository;
            _mapper = mapper;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Result<PaginatedList<TripMemberDto>>> Handle(GetTripMembersQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Get trip members attempt. TripId: {TripId}, Page: {PageNumber}, PageSize: {PageSize}",
                request.TripId,
                request.PageNumber,
                request.PageSize);

            if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
            {
                _logger.LogWarning("Get trip members failed: Invalid or missing user ID");
                return Result<PaginatedList<TripMemberDto>>.Failure(DomainErrors.Auth.InvalidToken);
            }

            _logger.LogDebug("User authenticated. UserId: {UserId}", userId);

            var trip = await _tripRepository.GetByIdAsync(request.TripId);
            if (trip == null)
            {
                _logger.LogWarning("Get trip members failed: Trip not found. TripId: {TripId}", request.TripId);
                return Result<PaginatedList<TripMemberDto>>.Failure(DomainErrors.Trip.NotFoundById(request.TripId));
            }

            if (trip.IsDeleted)
            {
                _logger.LogWarning("Get trip members failed: Trip is deleted. TripId: {TripId}", request.TripId);
                return Result<PaginatedList<TripMemberDto>>.Failure(DomainErrors.Trip.TripDeleted(request.TripId));
            }

            var currentUserMember = await _tripMemberRepository.GetByTripAndUserAsync(request.TripId, userId, cancellationToken);
            var isOwner = trip.UserId == userId;

            if (!isOwner && currentUserMember == null)
            {
                _logger.LogWarning("Get trip members failed: Access denied. UserId: {UserId}, TripId: {TripId}", userId, request.TripId);
                return Result<PaginatedList<TripMemberDto>>.Failure(DomainErrors.Trip.AccessDenied);
            }

            var query = _tripMemberRepository.GetMembersByTripIdQuery(request.TripId);

            var paginatedMembers = await query.ToPaginatedListAsync(
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var memberDtos = paginatedMembers.Items.Select(m => _mapper.Map<TripMemberDto>(m)).ToList();

            var result = PaginatedList<TripMemberDto>.Create(
                memberDtos,
                paginatedMembers.TotalCount,
                request.PageNumber,
                request.PageSize);

            _logger.LogInformation(
                "Successfully fetched {MemberCount} members (Page {PageNumber}/{TotalPages}) for TripId: {TripId}",
                memberDtos.Count,
                request.PageNumber,
                result.TotalPages,
                request.TripId);

            return Result<PaginatedList<TripMemberDto>>.Success(result);
        }
    }
}
