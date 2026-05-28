using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Vivu.Application.Interfaces.Cache;
using Vivu.Application.UseCases.Locations.Queries.GetPopularLocations;

namespace Vivu.Infrastructure.Services.Cache
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ILogger<RedisCacheService> _logger;

        public RedisCacheService(
            IDistributedCache distributedCache,
            IConnectionMultiplexer connectionMultiplexer,
            ILogger<RedisCacheService> logger)
        {
            _distributedCache = distributedCache;
            _connectionMultiplexer = connectionMultiplexer;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var cachedData = await _distributedCache.GetStringAsync(key, cancellationToken);

                if (string.IsNullOrWhiteSpace(cachedData))
                    return default;

                var jsonOptions = new JsonSerializerOptions
                {
                    NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                    WriteIndented = false 
                };

                return JsonSerializer.Deserialize<T>(cachedData, jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cached data for key: {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? expiration = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                    WriteIndented = false
                };
                var serializedData = JsonSerializer.Serialize(value, jsonOptions);

                var options = new DistributedCacheEntryOptions();

                if (expiration.HasValue)
                {
                    options.AbsoluteExpirationRelativeToNow = expiration.Value;
                }
                else
                {
                    options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1); 
                }

                await _distributedCache.SetStringAsync(key, serializedData, options, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cached data for key: {Key} with message {message}", key, ex.Message);
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                await _distributedCache.RemoveAsync(key, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cached data for key: {Key}", key);
            }
        }

        public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            try
            {
                var database = _connectionMultiplexer.GetDatabase();
                var endpoints = _connectionMultiplexer.GetEndPoints();
                var server = _connectionMultiplexer.GetServer(endpoints.First());

                // Scan for keys with prefix
                var keys = server.Keys(pattern: $"{prefix}*").ToArray();

                if (keys.Any())
                {
                    await database.KeyDeleteAsync(keys);
                    _logger.LogInformation("Removed {Count} keys with prefix: {Prefix}", keys.Length, prefix);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cached data by prefix: {Prefix}", prefix);
            }
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var cachedData = await _distributedCache.GetStringAsync(key, cancellationToken);
                return !string.IsNullOrWhiteSpace(cachedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if key exists: {Key}", key);
                return false;
            }
        }
        public string BuildCacheKey(GetPopularLocationsQuery request, string cacheKeyPrefix)
        {
            var parts = new List<string> { cacheKeyPrefix };

            if (request.CityId.HasValue)
            {
                parts.Add($"City_{request.CityId.Value}");
            }

            parts.Add(request.CategoryId.HasValue
                ? $"Category_{request.CategoryId.Value}"
                : "All");

            parts.Add($"Limit_{request.Limit}");

            return string.Join(":", parts);
        }
    }
}
