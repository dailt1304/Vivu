using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.UseCases.Locations.Queries.GetLocationBySearchText;
using Vivu.Application.UseCases.Locations.Queries.GetLocationsWithFilters;
using Vivu.Application.UseCases.Locations.Queries.GetNearbyLocations;
using Vivu.Domain.Entities;

namespace Vivu.Application.Interfaces.Locations
{
    public interface IFilterLocation
    {
        IQueryable<Location> ApplyFiltersToGetLocation( IQueryable<Location> query, GetLocationsByFilterQuery request);
        IQueryable<LocationDto> ApplySorting(IQueryable<LocationDto> query, GetLocationsByFilterQuery request);
        IQueryable<Location> ApplyFiltersToGetSearchLocation(IQueryable<Location> query,SearchLocationsQuery request);
        IQueryable<Location> ApplyFiltersToGetNearbyLocation(IQueryable<Location> query, GetNearbyLocationsQuery request);
    }
}
