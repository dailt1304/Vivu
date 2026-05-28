using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.UseCases.Locations.Queries.GetPopularLocations;

namespace Vivu.Application.Interfaces.Cache
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
        string BuildCacheKey(GetPopularLocationsQuery request, string cacheKeyPrefix);
    }
}
