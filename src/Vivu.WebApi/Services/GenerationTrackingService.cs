using Microsoft.Extensions.Caching.Memory;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.Interfaces.AI;

namespace Vivu.WebApi.Services
{
    /// <summary>
    /// Implementation of IGenerationTrackingService using IMemoryCache.
    /// Tracks background AI trip generation progress so the frontend can
    /// poll for status after a browser reload (race condition fallback).
    ///
    /// Entries auto-expire after 10 minutes — no manual cleanup needed.
    /// </summary>
    public class GenerationTrackingService : IGenerationTrackingService
    {
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(10);
        private const string KeyPrefix = "gen:";

        public GenerationTrackingService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public void SetProcessing(string generationId, string userId)
        {
            _cache.Set($"{KeyPrefix}{generationId}", new GenerationStatusDto
            {
                Status = "processing",
                UserId = userId
            }, CacheExpiry);
        }

        public void SetCompleted(string generationId, string userId, string tripId)
        {
            _cache.Set($"{KeyPrefix}{generationId}", new GenerationStatusDto
            {
                Status = "completed",
                TripId = tripId,
                UserId = userId
            }, CacheExpiry);
        }

        public void SetFailed(string generationId, string userId, string error)
        {
            _cache.Set($"{KeyPrefix}{generationId}", new GenerationStatusDto
            {
                Status = "failed",
                Error = error,
                UserId = userId
            }, CacheExpiry);
        }

        public GenerationStatusDto? GetStatus(string generationId, string userId)
        {
            if (!_cache.TryGetValue($"{KeyPrefix}{generationId}", out GenerationStatusDto? status))
                return null;

            // Security: verify the caller owns this generation
            if (status?.UserId != userId)
                return null;

            return status;
        }

        public GenerationStatusDto? GetStatus(string generationId)
        {
            if (!_cache.TryGetValue($"{KeyPrefix}{generationId}", out GenerationStatusDto? status))
                return null;

            return status;
        }
    }
}
