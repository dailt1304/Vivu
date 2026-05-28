using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MediatR;
using NetTopologySuite.Geometries;
using Vivu.Application.Common.Extensions;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Cities;
using Vivu.Application.DTOs.Responses.LocationCategories;
using Vivu.Application.DTOs.Responses.LocationDetails;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Locations;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Queries.GetLocationsWithFilters
{
    public class GetLocationsByFilterQueryHandler : IRequestHandler<GetLocationsByFilterQuery, Result<PaginatedList<LocationDto>>>
    {
        private readonly ILocationRepository _locationRepository;
        private readonly IFilterLocation _filterLocation;
        private readonly GeometryFactory _geometryFactory;

        public GetLocationsByFilterQueryHandler(ILocationRepository locationRepository, IFilterLocation filterLocation)
        {
            _locationRepository = locationRepository;
            _filterLocation = filterLocation;
            _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        }

        public async Task<Result<PaginatedList<LocationDto>>> Handle(
            GetLocationsByFilterQuery request,
            CancellationToken cancellationToken)
        {
            var query = _locationRepository.GetAllLocationsQuery();

            query = _filterLocation.ApplyFiltersToGetLocation(query, request);

            // Deduplicate by Name cleanly before sorting to preserve OrderBy semantics
            var distinctLocationIds = query
                .GroupBy(l => l.Name)
                .Select(g => g.OrderBy(l => l.Id).Select(x => x.Id).FirstOrDefault());
            
            query = query.Where(l => distinctLocationIds.Contains(l.Id));

            Point? userPoint = null;
            if (request.UserLatitude.HasValue && request.UserLongitude.HasValue)
            {
                userPoint = _geometryFactory.CreatePoint(
                    new Coordinate(request.UserLongitude.Value, request.UserLatitude.Value));
            }

            var projectedQuery = query.Select(l => new LocationDto
            {
                Id = l.Id,
                Name = l.Name,
                Description = l.Description,
                Address = l.Address,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                CityId = l.CityId,
                City = l.City != null ? new CityDto
                {
                    Id = l.City.Id,
                    Name = l.City.Name,
                    CountryId = l.City.CountryId,
                    Latitude = l.City.Latitude,
                    Longitude = l.City.Longitude
                } : null,
                CategoryId = l.CategoryId,
                Category = l.Category != null ? new LocationCategoryDto
                {
                    Id = l.Category.Id,
                    Name = l.Category.Name,
                    IconUrl = l.Category.IconUrl
                } : null,
                RatingAverage = l.RatingAverage,
                RatingCount = l.RatingCount,
                IsVerified = l.IsVerified,
                CreatedAt = l.CreatedDate,

                DistanceInMeters = userPoint != null && l.LocationPoint != null
                    ? l.LocationPoint.Distance(userPoint)
                    : null,
                LocationDetail = l.LocationDetail != null ? new LocationDetailDto
                {
                    LocationId = l.LocationDetail.LocationId,
                    OpeningHours = l.LocationDetail.OpeningHours,
                    Phone = l.LocationDetail.Phone,
                    Website = l.LocationDetail.Website,
                    Tags = l.LocationDetail.Tags,
                    Images = l.LocationDetail.Images
                } : null
            });

            projectedQuery = _filterLocation.ApplySorting(projectedQuery, request);

            var result = await projectedQuery.ToPaginatedListAsync(
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            return Result<PaginatedList<LocationDto>>.Success(result);
        }

    }
}
