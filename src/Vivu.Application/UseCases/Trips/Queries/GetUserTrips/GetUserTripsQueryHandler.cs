using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Application.Common.Extensions;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Trips;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Queries.GetUserTrips
{
    public class GetUserTripsQueryHandler : IRequestHandler<GetUserTripsQuery, Result<UserTripsResponseDto>>
    {
        private readonly ITripRepository _tripRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITripLimitChecker _tripLimitChecker;
        private readonly IMapper _mapper;
        private readonly ILogger<GetUserTripsQueryHandler> _logger;

        public GetUserTripsQueryHandler(
            ITripRepository tripRepository,
            IUserRepository userRepository,
            ITripLimitChecker tripLimitChecker,
            IMapper mapper,
            ILogger<GetUserTripsQueryHandler> logger)
        {
            _tripRepository = tripRepository;
            _userRepository = userRepository;
            _tripLimitChecker = tripLimitChecker;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<UserTripsResponseDto>> Handle(
            GetUserTripsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Fetching trips for user: {UserId}, Status: {Status}, Page: {PageNumber}, PageSize: {PageSize}",
                request.UserId,
                request.Status ?? "All",
                request.PageNumber,
                request.PageSize);

            // Get user to check if premium
            var user = await _userRepository.GetByIdAsync(request.UserId);
            var isPremium = user?.HasRole(Role.Names.Premium) ?? false;

            // Get trip count and limit
            var numberOfTripCreated = await _tripLimitChecker.GetTripCountAsync(request.UserId, cancellationToken);
            var tripLimit = _tripLimitChecker.GetTripLimit(isPremium);

            var query = _tripRepository.GetTripsByUserIdQuery(request.UserId);

            if (query == null || !await query.AnyAsync(cancellationToken))
            {
                _logger.LogInformation("No trips found for user {UserId}", request.UserId);
                
                var emptyResult = new PaginatedList<TripDto>(
                    new List<TripDto>(),
                    count: 0,
                    request.PageNumber,
                    request.PageSize);
                
                return Result<UserTripsResponseDto>.Success(new UserTripsResponseDto
                {
                    Trips = emptyResult,
                    NumberOfTripCreated = numberOfTripCreated,
                    TripLimit = tripLimit
                });
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var statusLower = request.Status.ToLower();
                query = query.Where(t => t.Status.ToLower() == statusLower);
                
                _logger.LogDebug("Applied status filter: {Status}", statusLower);
            }

            query = query.OrderByDescending(t => t.CreatedDate);

            var paginatedTrips = await query.ToPaginatedListAsync(
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            _logger.LogDebug(
                "Retrieved {Count} trips out of {TotalCount} for user {UserId}",
                paginatedTrips.Items.Count,
                paginatedTrips.TotalCount,
                request.UserId);

            // Mapping with current user context
            var tripDtos = paginatedTrips.Items.Select(trip => 
                _mapper.Map<TripDto>(trip, opts => opts.Items["CurrentUserId"] = request.UserId)
            ).ToList();

            var paginatedResult = new PaginatedList<TripDto>(
                tripDtos,
                paginatedTrips.TotalCount,
                paginatedTrips.PageNumber,
                paginatedTrips.PageSize);

            _logger.LogInformation(
                "Successfully fetched {Count} trips for user {UserId}",
                paginatedResult.Items.Count,
                request.UserId);

            return Result<UserTripsResponseDto>.Success(new UserTripsResponseDto
            {
                Trips = paginatedResult,
                NumberOfTripCreated = numberOfTripCreated,
                TripLimit = tripLimit
            });
        }
    }
}
