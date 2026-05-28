using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.Common.Extensions;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Queries.GetPendingLocations
{
    public class GetPendingLocationsQueryHandler : IRequestHandler<GetPendingLocationsQuery, Result<PaginatedList<LocationDto>>>
    {
        private readonly ILocationRepository _locationRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetPendingLocationsQueryHandler> _logger;

        public GetPendingLocationsQueryHandler(
            ILocationRepository locationRepository,
            IMapper mapper,
            ILogger<GetPendingLocationsQueryHandler> logger)
        {
            _locationRepository = locationRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<PaginatedList<LocationDto>>> Handle(GetPendingLocationsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Get pending locations attempt. Page: {PageNumber}, PageSize: {PageSize}",
                request.PageNumber,
                request.PageSize);

            var query = _locationRepository.GetPendingLocationsQuery();

            var paginatedLocations = await query.ToPaginatedListAsync(
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            if (paginatedLocations.TotalCount == 0 || paginatedLocations.Items.Count == 0)
            {
                var emptyResult = new PaginatedList<LocationDto>(
                    new List<LocationDto>(),
                    paginatedLocations.TotalCount,
                    paginatedLocations.PageNumber,
                    paginatedLocations.PageSize);

                _logger.LogInformation(
                    "No pending locations found. PageNumber: {PageNumber}, PageSize: {PageSize}",
                    emptyResult.PageNumber,
                    emptyResult.PageSize);

                return Result<PaginatedList<LocationDto>>.Success(emptyResult);
            }

            var locationDtos = _mapper.Map<List<LocationDto>>(paginatedLocations.Items);

            var result = new PaginatedList<LocationDto>(
                locationDtos,
                paginatedLocations.TotalCount,
                paginatedLocations.PageNumber,
                paginatedLocations.PageSize);

            _logger.LogInformation(
                "Pending locations retrieved successfully. TotalCount: {TotalCount}, PageNumber: {PageNumber}, PageSize: {PageSize}",
                result.TotalCount,
                result.PageNumber,
                result.PageSize);

            return Result<PaginatedList<LocationDto>>.Success(result);
        }
    }
}