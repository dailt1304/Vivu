using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NpgsqlTypes;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Locations;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Queries.GetLocationBySearchText
{
    public class SearchLocationsQueryHandler
     : IRequestHandler<SearchLocationsQuery, Result<PaginatedList<LocationSearchResultDto>>>
    {
        private readonly ILocationRepository _locationRepository;
        private readonly IFilterLocation _filterLocation;
        private readonly ISearchTermLocation _searchTermLocation;
        private readonly ILogger<SearchLocationsQueryHandler> _logger;

        public SearchLocationsQueryHandler(ILocationRepository locationRepository, IFilterLocation filterLocation, 
                                            ISearchTermLocation searchTermLocation, ILogger<SearchLocationsQueryHandler> logger)
        {
            _filterLocation = filterLocation;
            _locationRepository = locationRepository;
            _searchTermLocation = searchTermLocation;
            _logger = logger;
        }

        public async Task<Result<PaginatedList<LocationSearchResultDto>>> Handle(
            SearchLocationsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Search location with {searchterm}", request.SearchTerm);
            var searchTerm = _searchTermLocation.SanitizeSearchTerm(request.SearchTerm);

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                _logger.LogWarning("Search term after sanitize is null or empty: {sanitize}", searchTerm);
                return Result<PaginatedList<LocationSearchResultDto>>.Failure(
                    DomainErrors.Location.InvalidSearchTerm);
            }
            _logger.LogDebug("Search term sanitized: {sanitized}", searchTerm);

            var tsQuery = _searchTermLocation.ConvertToTsQuery(searchTerm);
            _logger.LogDebug("Convert sanitizd to ts query from {sanitized} to {tsquery}", searchTerm, tsQuery); ;
            var query = _locationRepository.GetSearchTermLocation(tsQuery);
            _logger.LogDebug("Found {count} location after apply search term location", query.Count());
            query = _filterLocation.ApplyFiltersToGetSearchLocation(query, request);
            var totalCount = await query.CountAsync(cancellationToken);
            _logger.LogDebug("Found {count} location after apply apply filter", totalCount);

            var projectedQuery = query.Select(l => new LocationSearchResultDto
            {
                Id = l.Id,
                Name = l.Name,
                Description = l.Description,
                Address = l.Address,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                CityId = l.CityId,
                CityName = l.City != null ? l.City.Name : null,
                CategoryId = l.CategoryId,
                CategoryName = l.Category != null ? l.Category.Name : null,
                CategoryIconUrl = l.Category != null ? l.Category.IconUrl : null,
                RatingAverage = l.RatingAverage,
                RatingCount = l.RatingCount,
                IsVerified = l.IsVerified,
                Images = l.LocationDetail != null ? l.LocationDetail.Images : null,
                Relevance = EF.Property<NpgsqlTsVector>(l, "SearchVector")
            .Rank(EF.Functions.ToTsQuery("simple", tsQuery))
            });
            _logger.LogDebug("Query mapped to LocationSearchResulttDto");
            projectedQuery = projectedQuery.OrderByDescending(x =>
                x.Relevance + (float)(x.RatingAverage / 5.0m * 0.2m));
            _logger.LogDebug("LocationSearchResultDto ordered by relevance and rating average");
            var items = await projectedQuery
                .Skip(request.Skip)
                .Take(request.Take)
                .ToListAsync(cancellationToken);
            _logger.LogDebug("Skip {skip} and take {take} items", request.Skip, request.Take);
            if (request.IncludeHighlights && items.Any())
            {
                items = _searchTermLocation.ApplyHighlightingInMemory(items, searchTerm);
                _logger.LogDebug("Apply highlight in memory with search term {searchTerm}", searchTerm);
            }

            var paginatedList = new PaginatedList<LocationSearchResultDto>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize);
            _logger.LogDebug("Successfully fetched {Count} locations", paginatedList.TotalCount);

            return Result<PaginatedList<LocationSearchResultDto>>.Success(paginatedList);
        }
    }
}
