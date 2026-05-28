using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Application.Common.Extensions;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Trips;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Queries.GetPublicTrips
{
    public class GetPublicTripsQueryHandler : IRequestHandler<GetPublicTripsQuery, Result<PaginatedList<PublicTripDto>>>
    {
        private readonly ITripRepository _tripRepository;
        private readonly ICityRepository _cityRepository;
        private readonly IPublicTripService _publicTripService;
        private readonly IMapper _mapper;
        private readonly ILogger<GetPublicTripsQueryHandler> _logger;

        public GetPublicTripsQueryHandler(
            ITripRepository tripRepository,
            ICityRepository cityRepository,
            IPublicTripService publicTripService,
            IMapper mapper,
            ILogger<GetPublicTripsQueryHandler> logger)
        {
            _tripRepository = tripRepository;
            _cityRepository = cityRepository;
            _publicTripService = publicTripService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<PaginatedList<PublicTripDto>>> Handle(
            GetPublicTripsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Fetching public trips - CityId: {CityId}, CountryId: {CountryId}, Duration: {Duration}, SortBy: {SortBy}, Page: {PageNumber}, PageSize: {PageSize}",
                request.CityId,
                request.CountryId,
                request.Duration,
                request.SortBy,
                request.PageNumber,
                request.PageSize);

            if (request.CityId.HasValue)
            {
                var cityExists = await _cityRepository.GetByIdAsync(request.CityId.Value);
                if (cityExists == null)
                {
                    _logger.LogWarning("City not found: {CityId}", request.CityId.Value);
                    return Result<PaginatedList<PublicTripDto>>.Failure(
                        DomainErrors.Cities.NotFoundById(request.CityId.Value));
                }
            }

            var query = _tripRepository.GetPublicTripsQuery();

            if (query == null || !await query.AnyAsync(cancellationToken))
            {
                _logger.LogInformation("No public trips found");
                
                var emptyResult = new PaginatedList<PublicTripDto>(
                    new List<PublicTripDto>(),
                    count: 0,
                    request.PageNumber,
                    request.PageSize);
                
                return Result<PaginatedList<PublicTripDto>>.Success(emptyResult);
            }

            // Filter by city if provided
            if (request.CityId.HasValue)
            {
                query = query.Where(t => t.CityId == request.CityId.Value);
                _logger.LogDebug("Applied city filter: {CityId}", request.CityId.Value);
            }

            // Filter by country if provided
            if (request.CountryId.HasValue)
            {
                query = query.Where(t => t.City != null && t.City.CountryId == request.CountryId.Value);
                _logger.LogDebug("Applied country filter: {CountryId}", request.CountryId.Value);
            }

            // Filter by duration if provided
            if (request.Duration.HasValue)
            {
                query = _publicTripService.ApplyDurationFilter(query, request.Duration.Value);
                _logger.LogDebug("Applied duration filter: {Duration} days", request.Duration);
            }

            query = _publicTripService.ApplySorting(query, request.SortBy ?? "newest");
            _logger.LogDebug("Applied sorting: {SortBy}", request.SortBy);

            var paginatedTrips = await query.ToPaginatedListAsync(
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            _logger.LogDebug(
                "Retrieved {Count} public trips out of {TotalCount}",
                paginatedTrips.Items.Count,
                paginatedTrips.TotalCount);

            var tripDtos = paginatedTrips.Items.Select(trip => 
                _mapper.Map<PublicTripDto>(trip, opt => 
                    opt.Items["PublicTripService"] = _publicTripService)
            ).ToList();

            var result = new PaginatedList<PublicTripDto>(
                tripDtos,
                paginatedTrips.TotalCount,
                paginatedTrips.PageNumber,
                paginatedTrips.PageSize);

            _logger.LogInformation(
                "Successfully fetched {Count} public trips",
                result.Items.Count);

            return Result<PaginatedList<PublicTripDto>>.Success(result);
        }
    }
}
