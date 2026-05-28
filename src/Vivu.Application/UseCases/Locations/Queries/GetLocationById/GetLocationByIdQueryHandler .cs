using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Vivu.Application.DTOs.Responses.Cities;
using Vivu.Application.DTOs.Responses.LocationCategories;
using Vivu.Application.DTOs.Responses.LocationDetails;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.UseCases.Locations.Queries.GetAllLocations;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Queries.GetLocatioinById
{
    public class GetLocationByIdQueryHandler
    : IRequestHandler<GetLocationByIdQuery, Result<LocationDto>>
    {
        private readonly ILocationRepository _locationRepository;
        private readonly IMapper _mapper;
        private const double NearbyRadiusInMeters = 0.5;
        private readonly ILogger<GetLocationByIdQueryHandler> _logger;

        public GetLocationByIdQueryHandler(ILocationRepository locationRepository, IMapper mapper,
                                            ILogger<GetLocationByIdQueryHandler> logger)
        {
            _mapper = mapper;
            _logger = logger;
            _locationRepository = locationRepository;
        }

        public async Task<Result<LocationDto>> Handle(
            GetLocationByIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching detail with location {id}", request.LocationId);
            var location = await _locationRepository.GetByIdAsync(request.LocationId);

            if (location == null)
            {
                _logger.LogWarning("Location {id} was not found", request.LocationId);
                return Result<LocationDto>.Failure(
                    DomainErrors.Location.NotFoundById(request.LocationId));
            }

            if (!location.IsVerified)
            {
                _logger.LogWarning("Location {id} was not verified", request.LocationId);
                return Result<LocationDto>.Failure(
                    DomainErrors.Location.NotVerified);
            }
            _logger.LogDebug("Location {id} fetched", location.Id);
            var images = new List<string>();
            if (location.LocationDetail?.Images != null)
            {
                try
                {
                    images = JsonSerializer.Deserialize<List<string>>(location.LocationDetail.Images)
                             ?? new List<string>();
                }
                catch
                {
                    _logger.LogWarning("Cannot deserialize images for location {id}", location.Id);
                    images = new List<string>();
                }
            }

            var nearbyLocations = new List<NearbyLocationDto>();
            if (location.LocationPoint != null)
            {
                var locations = await _locationRepository.GetNearbyLocationsAsync(
                    location.LocationPoint,
                    location.Id,
                    NearbyRadiusInMeters,
                    cancellationToken);
                _logger.LogDebug($"Found nearby locations: {locations.Count}");
                foreach ( var nearby in locations )
                {
                    var locationDto = _mapper.Map<NearbyLocationDto>(nearby);
                    locationDto.DistanceInMeters = nearby.GetDistanceInMeters(location.LocationPoint);
                    nearbyLocations.Add(locationDto);
                }
            }
            _logger.LogDebug("Fetched {count} location nearby", nearbyLocations.Count);
            var response = _mapper.Map<LocationDto>(location);
            response.NearbyLocations = nearbyLocations;

            return Result<LocationDto>.Success(response);
        }
    }
}
