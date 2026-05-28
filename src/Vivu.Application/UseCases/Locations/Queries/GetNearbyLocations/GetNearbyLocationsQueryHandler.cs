using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Locations;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Queries.GetNearbyLocations
{
    public class GetNearbyLocationsQueryHandler
    : IRequestHandler<GetNearbyLocationsQuery, Result<List<NearbyLocationDto>>>
    {
        private readonly ILocationRepository _locationRepository;
        private readonly IMapper _mapper;
        private readonly IFilterLocation _filterLocation;
        private readonly GeometryFactory _geometryFactory;

        public GetNearbyLocationsQueryHandler(ILocationRepository locationRepository, IFilterLocation filterLocation,
                                                IMapper mapper)
        {
            _locationRepository = locationRepository;
            _filterLocation = filterLocation;
            _mapper = mapper;
            _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        }

        public async Task<Result<List<NearbyLocationDto>>> Handle(
            GetNearbyLocationsQuery request,
            CancellationToken cancellationToken)
        {
            var userPoint = _geometryFactory.CreatePoint(
                new Coordinate(request.Longitude, request.Latitude));

            var query = _locationRepository.GetPointLocation(request.Limit, userPoint);

            query = _filterLocation.ApplyFiltersToGetNearbyLocation(query, request);

            double radiusInDegrees = request.RadiusInMeters / 111320.0;

            query = query.Where(l =>
                l.LocationPoint!.IsWithinDistance(userPoint, radiusInDegrees));

            var locations = await query.ToListAsync(cancellationToken);

            var nearbyLocations = _mapper.Map<List<NearbyLocationDto>>(locations, opts =>
            {
                opts.Items["UserPoint"] = userPoint;
            });

            return Result<List<NearbyLocationDto>>.Success(nearbyLocations);
        }
    }
}
