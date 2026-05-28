using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Application.Common.Extensions;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Queries.GetAllLocations
{
    public class GetAllLocationsQueryHandler : IRequestHandler<GetAllLocationsQuery, Result<PaginatedList<LocationDto>>>
    {
        private readonly ILocationRepository _locationRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetAllLocationsQueryHandler> _logger;

        public GetAllLocationsQueryHandler(
            ILocationRepository locationRepository,
            IMapper mapper,
            ILogger<GetAllLocationsQueryHandler> logger)
        {
            _locationRepository = locationRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<PaginatedList<LocationDto>>> Handle(
            GetAllLocationsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Fetching locations, Page: {PageNumber}, PageSize: {PageSize}",
                request.PageNumber,
                request.PageSize);

            var query = _locationRepository.GetAllLocationsQuery();

            if (query == null || !await query.AnyAsync(cancellationToken))
            {
                _logger.LogInformation("No locations found");
                
                var emptyResult = new PaginatedList<LocationDto>(
                    new List<LocationDto>(),
                    count: 0,
                    request.PageNumber,
                    request.PageSize);
                
                return Result<PaginatedList<LocationDto>>.Success(emptyResult);
            }

            query = query.OrderBy(l => l.Name);

            var paginatedLocations = await query.ToPaginatedListAsync(
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            _logger.LogDebug(
                "Retrieved {Count} locations out of {TotalCount}",
                paginatedLocations.Items.Count,
                paginatedLocations.TotalCount);

            // Mapping
            var locationDtos = paginatedLocations.Items.Select(location => _mapper.Map<LocationDto>(location)).ToList();

            var result = new PaginatedList<LocationDto>(
                locationDtos,
                paginatedLocations.TotalCount,
                paginatedLocations.PageNumber,
                paginatedLocations.PageSize);

            _logger.LogInformation(
                "Successfully fetched {Count} locations",
                result.Items.Count);

            return Result<PaginatedList<LocationDto>>.Success(result);
        }
    }
}
