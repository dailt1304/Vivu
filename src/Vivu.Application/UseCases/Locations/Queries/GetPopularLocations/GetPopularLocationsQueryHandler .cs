using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Cache;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Queries.GetPopularLocations
{
    public class GetPopularLocationsQueryHandler
     : IRequestHandler<GetPopularLocationsQuery, Result<List<PopularLocationDto>>>
    {
        private readonly ILocationRepository _locationRepository;
        private readonly ICacheService _cacheService;
        private const string CacheKeyPrefix = "PopularLocations_v2";
        private readonly IMapper _mapper;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

        public GetPopularLocationsQueryHandler(
            ILocationRepository locationRepository,
            IMapper mapper,
            ICacheService cacheService) 
        {
            _cacheService = cacheService;
            _mapper = mapper;
            _locationRepository = locationRepository;
        }

        public async Task<Result<List<PopularLocationDto>>> Handle(
            GetPopularLocationsQuery request,
            CancellationToken cancellationToken)
        {
            var cacheKey = _cacheService.BuildCacheKey(request,CacheKeyPrefix);

            var cachedResult = await _cacheService.GetAsync<List<PopularLocationDto>>(
                cacheKey,
                cancellationToken);

            if (cachedResult != null && cachedResult.Any())
            {
                return Result<List<PopularLocationDto>>.Success(cachedResult);
            }

            var query = _locationRepository.GetVerifyLocations();

            if (request.CityId.HasValue)
            {
                query = query.Where(l => l.CityId == request.CityId.Value);
            }

            if (request.CategoryId.HasValue)
            {
                query = query.Where(l => l.CategoryId == request.CategoryId.Value);
            }

            // Deduplicate by Name (same pattern as GetLocationsByFilterQueryHandler)
            var distinctLocationIds = query
                .GroupBy(l => l.Name)
                .Select(g => g.OrderBy(l => l.Id).Select(x => x.Id).FirstOrDefault());
            query = query.Where(l => distinctLocationIds.Contains(l.Id));

            var popularLocations = await _locationRepository.GetPopularLocations(query, request.Limit, cancellationToken);

            var result = _mapper.Map<List<PopularLocationDto>>(popularLocations);
            await _cacheService.SetAsync(
                cacheKey,
                result,
                CacheDuration,
                cancellationToken);

            return Result<List<PopularLocationDto>>.Success(result);
        }

    }
}
