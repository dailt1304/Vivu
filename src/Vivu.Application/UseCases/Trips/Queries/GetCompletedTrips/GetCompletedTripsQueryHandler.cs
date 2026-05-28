using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Application.Common.Extensions;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Queries.GetCompletedTrips
{
    public class GetCompletedTripsQueryHandler : IRequestHandler<GetCompletedTripsQuery, Result<PaginatedList<TripDto>>>
    {
        private readonly ITripRepository _tripRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetCompletedTripsQueryHandler> _logger;

        public GetCompletedTripsQueryHandler(
            ITripRepository tripRepository,
            IMapper mapper,
            ILogger<GetCompletedTripsQueryHandler> logger)
        {
            _tripRepository = tripRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<PaginatedList<TripDto>>> Handle(
            GetCompletedTripsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Fetching completed trips for user: {UserId}, Page: {PageNumber}, PageSize: {PageSize}",
                request.UserId,
                request.PageNumber,
                request.PageSize);

            var query = _tripRepository.GetTripsByUserIdQuery(request.UserId);

            if (query == null || !await query.AnyAsync(cancellationToken))
            {
                _logger.LogInformation("No trips found for user {UserId}", request.UserId);
                
                var emptyResult = new PaginatedList<TripDto>(
                    new List<TripDto>(),
                    count: 0,
                    request.PageNumber,
                    request.PageSize);
                
                return Result<PaginatedList<TripDto>>.Success(emptyResult);
            }

            // Filter only completed trips
            query = query.Where(t => t.Status.ToLower() == "completed");
            
            _logger.LogDebug("Applied status filter: completed");

            query = query.OrderByDescending(t => t.CreatedDate);

            var paginatedTrips = await query.ToPaginatedListAsync(
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            _logger.LogDebug(
                "Retrieved {Count} completed trips out of {TotalCount} for user {UserId}",
                paginatedTrips.Items.Count,
                paginatedTrips.TotalCount,
                request.UserId);

            // Mapping
            var tripDtos = paginatedTrips.Items.Select(trip => 
                _mapper.Map<TripDto>(trip, opt => opt.Items["CurrentUserId"] = request.UserId)
            ).ToList();

            var result = new PaginatedList<TripDto>(
                tripDtos,
                paginatedTrips.TotalCount,
                paginatedTrips.PageNumber,
                paginatedTrips.PageSize);

            _logger.LogInformation(
                "Successfully fetched {Count} completed trips for user {UserId}",
                result.Items.Count,
                request.UserId);

            return Result<PaginatedList<TripDto>>.Success(result);
        }
    }
}
