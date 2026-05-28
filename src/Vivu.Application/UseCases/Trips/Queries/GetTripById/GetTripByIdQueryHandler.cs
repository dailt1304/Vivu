using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Queries.GetTripById
{
    public class GetTripByIdQueryHandler : IRequestHandler<GetTripByIdQuery, Result<DetailedTripDto>>
    {
        private readonly ITripRepository _tripRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetTripByIdQueryHandler> _logger;

        public GetTripByIdQueryHandler(
            ITripRepository tripRepository,
            IMapper mapper,
            ILogger<GetTripByIdQueryHandler> logger)
        {
            _tripRepository = tripRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<DetailedTripDto>> Handle(
            GetTripByIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Fetching trip details for TripId: {TripId}, RequestUserId: {UserId}",
                request.TripId,
                request.RequestUserId);

            // Fetch trip with all related data
            var trip = await _tripRepository.GetTripByIdWithDetailsAsync(request.TripId, cancellationToken);

            if (trip == null)
            {
                _logger.LogWarning("Trip with ID {TripId} not found", request.TripId);
                return Result<DetailedTripDto>.Failure(DomainErrors.Trip.NotFoundById(request.TripId));
            }

            // Permission check: User must be owner or member
            var isOwner = trip.UserId == request.RequestUserId;
            var isMember = trip.TripMembers.Any(m => m.UserId == request.RequestUserId);

            if (!isOwner && !isMember)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to access trip {TripId} without permission",
                    request.RequestUserId,
                    request.TripId);
                return Result<DetailedTripDto>.Failure(DomainErrors.Trip.AccessDenied);
            }

            _logger.LogDebug("User {UserId} has access to trip {TripId}", request.RequestUserId, request.TripId);

            // Map to DetailedTripDto
            var detailedTripDto = _mapper.Map<DetailedTripDto>(trip);

            _logger.LogInformation(
                "Successfully fetched trip {TripId} with {MemberCount} members, {DayCount} days, {LocationCount} locations",
                trip.Id,
                detailedTripDto.MemberCount,
                detailedTripDto.DayCount,
                detailedTripDto.LocationCount);

            return Result<DetailedTripDto>.Success(detailedTripDto);
        }
    }
}
