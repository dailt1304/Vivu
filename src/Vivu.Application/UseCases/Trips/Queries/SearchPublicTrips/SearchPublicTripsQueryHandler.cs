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

namespace Vivu.Application.UseCases.Trips.Queries.SearchPublicTrips
{
    public class SearchPublicTripsQueryHandler : IRequestHandler<SearchPublicTripsQuery, Result<PaginatedList<PublicTripDto>>>
    {
        private readonly ITripRepository _tripRepository;
        private readonly ICityRepository _cityRepository;
        private readonly IPublicTripService _publicTripService;
        private readonly ISearchTermTrip _searchTermTrip;
        private readonly IMapper _mapper;
        private readonly ILogger<SearchPublicTripsQueryHandler> _logger;

        public SearchPublicTripsQueryHandler(
            ITripRepository tripRepository,
            ICityRepository cityRepository,
            IPublicTripService publicTripService,
            ISearchTermTrip searchTermTrip,
            IMapper mapper,
            ILogger<SearchPublicTripsQueryHandler> logger)
        {
            _tripRepository = tripRepository;
            _cityRepository = cityRepository;
            _publicTripService = publicTripService;
            _searchTermTrip = searchTermTrip;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<PaginatedList<PublicTripDto>>> Handle(
            SearchPublicTripsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Searching public trips - SearchTerm: {SearchTerm}, CityId: {CityId}, CountryId: {CountryId}, Duration: {Duration}, Page: {PageNumber}, PageSize: {PageSize}",
                request.SearchTerm,
                request.CityId,
                request.CountryId,
                request.Duration,
                request.PageNumber,
                request.PageSize);

            // Sanitize search term
            var searchTerm = _searchTermTrip.SanitizeSearchTerm(request.SearchTerm);

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                _logger.LogWarning("Search term after sanitization is null or empty: {SearchTerm}", searchTerm);
                return Result<PaginatedList<PublicTripDto>>.Failure(
                    DomainErrors.Trip.InvalidSearchTerm);
            }

            _logger.LogDebug("Search term sanitized: {SanitizedTerm}", searchTerm);

            // Convert to tsquery
            var tsQuery = _searchTermTrip.ConvertToTsQuery(searchTerm);
            _logger.LogDebug("Converted to tsquery: {TsQuery}", tsQuery);

            // Validate city if provided
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

            // Get trips matching search term
            var query = _tripRepository.GetSearchTermTrips(tsQuery);
            _logger.LogDebug("Found {Count} trips after applying search term", query.Count());

            // Apply filters
            if (request.CityId.HasValue)
            {
                query = query.Where(t => t.CityId == request.CityId.Value);
                _logger.LogDebug("Applied city filter: {CityId}", request.CityId.Value);
            }

            if (request.CountryId.HasValue)
            {
                query = query.Where(t => t.City != null && t.City.CountryId == request.CountryId.Value);
                _logger.LogDebug("Applied country filter: {CountryId}", request.CountryId.Value);
            }

            if (request.Duration.HasValue)
            {
                query = _publicTripService.ApplyDurationFilter(query, request.Duration.Value);
                _logger.LogDebug("Applied duration filter: {Duration} days", request.Duration);
            }

            // Apply sorting by relevance (search rank)
            query = query.OrderByDescending(t => 
                EF.Property<NpgsqlTypes.NpgsqlTsVector>(t, "SearchVector")
                    .Rank(EF.Functions.ToTsQuery("simple", tsQuery)));

            _logger.LogDebug("Applied relevance sorting");

            // Paginate
            var paginatedTrips = await query.ToPaginatedListAsync(
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            _logger.LogDebug(
                "Retrieved {Count} trips out of {TotalCount}",
                paginatedTrips.Items.Count,
                paginatedTrips.TotalCount);

            // Map to DTOs
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
                "Successfully searched {Count} public trips",
                result.Items.Count);

            return Result<PaginatedList<PublicTripDto>>.Success(result);
        }
    }
}
