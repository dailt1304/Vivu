using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Destinations;
using Vivu.Application.Interfaces.Cache;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;
using TripLocationEntity = Vivu.Domain.Entities.TripLocation;

namespace Vivu.Application.UseCases.Destinations.Queries.GetTrendingDestinations
{
    public class GetTrendingDestinationsQueryHandler
        : IRequestHandler<GetTrendingDestinationsQuery, Result<PaginatedList<TrendingDestinationDto>>>
    {
        private readonly ITripRepository _tripRepository;
        private readonly ITripDayRepository _tripDayRepository;
        private readonly ITripLocationRepository _tripLocationRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly ICityRepository _cityRepository;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetTrendingDestinationsQueryHandler> _logger;
        private const string CacheKeyPrefix = "TrendingDestinations";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

        public GetTrendingDestinationsQueryHandler(
            ITripRepository tripRepository,
            ITripDayRepository tripDayRepository,
            ITripLocationRepository tripLocationRepository,
            ILocationRepository locationRepository,
            ICityRepository cityRepository,
            ICacheService cacheService,
            ILogger<GetTrendingDestinationsQueryHandler> logger)
        {
            _tripRepository = tripRepository;
            _tripDayRepository = tripDayRepository;
            _tripLocationRepository = tripLocationRepository;
            _locationRepository = locationRepository;
            _cityRepository = cityRepository;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<PaginatedList<TrendingDestinationDto>>> Handle(
            GetTrendingDestinationsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Build cache key with pagination parameters
                var cacheKey = $"{CacheKeyPrefix}:Page{request.PageNumber}:Size{request.PageSize}:Locations{request.LocationsPerCity}";

                // Check cache first
                try
                {
                    var cachedResult = await _cacheService.GetAsync<PaginatedList<TrendingDestinationDto>>(
                        cacheKey,
                        cancellationToken);

                    if (cachedResult != null)
                    {
                        _logger.LogInformation("Returning trending destinations from cache");
                        return Result<PaginatedList<TrendingDestinationDto>>.Success(cachedResult);
                    }
                }
                catch (Exception cacheEx)
                {
                    _logger.LogWarning(cacheEx, "Failed to retrieve data from cache, continuing without cache");
                }

                // Calculate date 30 days ago
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

                // Get public trips from last 30 days with their city info
                var recentPublicTrips = await _tripRepository
                    .GetPublicTripsQuery()
                    .Where(t => t.CreatedDate >= thirtyDaysAgo && t.CityId.HasValue)
                    .Select(t => new
                    {
                        t.Id,
                        t.CityId,
                        CityName = t.City!.Name
                    })
                    .ToListAsync(cancellationToken);

                if (!recentPublicTrips.Any())
                {
                    _logger.LogInformation("No public trips found in the last 30 days");
                    var emptyResult = new PaginatedList<TrendingDestinationDto>(
                        new List<TrendingDestinationDto>(),
                        0,
                        request.PageNumber,
                        request.PageSize);
                    return Result<PaginatedList<TrendingDestinationDto>>.Success(emptyResult);
                }

                // Group by city and count trips (get all first for total count)
                var allTrendingCities = recentPublicTrips
                    .GroupBy(t => new { t.CityId, t.CityName })
                    .Select(g => new
                    {
                        CityId = g.Key.CityId!.Value,
                        g.Key.CityName,
                        TripCount = g.Count(),
                        TripIds = g.Select(t => t.Id).ToList()
                    })
                    .OrderByDescending(x => x.TripCount)
                    .ToList();

                // Apply pagination
                var totalCount = allTrendingCities.Count;
                var trendingCities = allTrendingCities
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                var result = new List<TrendingDestinationDto>();

                foreach (var city in trendingCities)
                {
                    // Get all trip days from trips in this city using GetByTripIdAsync
                    var tripDayIdsList = new List<Guid>();
                    foreach (var tripId in city.TripIds)
                    {
                        var tripDays = await _tripDayRepository.GetByTripIdAsync(tripId);
                        tripDayIdsList.AddRange(tripDays.Select(td => td.Id));
                    }

                    if (!tripDayIdsList.Any())
                    {
                        _logger.LogWarning("No trip days found for city {CityName} with {TripCount} trips", city.CityName, city.TripIds.Count);
                        result.Add(new TrendingDestinationDto
                        {
                            CityId = city.CityId,
                            CityName = city.CityName,
                            TripCount = city.TripCount,
                            TrendingLocations = new List<TrendingLocationDto>()
                        });
                        continue;
                    }

                    // Get all trip locations for these trip days
                    var allTripLocationsForCity = new List<TripLocationEntity>();
                    foreach (var tripDayId in tripDayIdsList)
                    {
                        var tripLocations = await _tripLocationRepository.GetTripLocationsByTripDayIdAsync(tripDayId);
                        allTripLocationsForCity.AddRange(tripLocations);
                    }

                    if (!allTripLocationsForCity.Any())
                    {
                        _logger.LogWarning("No trip locations found for city {CityName} with {DayCount} trip days", city.CityName, tripDayIdsList.Count);
                        result.Add(new TrendingDestinationDto
                        {
                            CityId = city.CityId,
                            CityName = city.CityName,
                            TripCount = city.TripCount,
                            TrendingLocations = new List<TrendingLocationDto>()
                        });
                        continue;
                    }

                    // Group and count locations
                    var trendingLocations = allTripLocationsForCity
                        .GroupBy(tl => tl.LocationId)
                        .Select(g => new
                        {
                            LocationId = g.Key,
                            TripCount = g.Count()
                        })
                        .OrderByDescending(x => x.TripCount)
                        .Take(request.LocationsPerCity)
                        .ToList();

                    // Get location details
                    var locationIds = trendingLocations.Select(tl => tl.LocationId).ToList();
                    var locations = await _locationRepository
                        .GetAllLocationsQuery()
                        .Where(l => locationIds.Contains(l.Id))
                        .Select(l => new
                        {
                            l.Id,
                            l.Name,
                            l.Description,
                            l.Address,
                            l.Latitude,
                            l.Longitude,
                            l.CategoryId,
                            CategoryName = l.Category != null ? l.Category.Name : null,
                            CategoryIconUrl = l.Category != null ? l.Category.IconUrl : null,
                            l.RatingAverage,
                            l.RatingCount,
                            ThumbnailUrl = l.LocationDetail != null ? l.LocationDetail.Images : null
                        })
                        .ToListAsync(cancellationToken);

                    var locationDtos = locations.Select(l =>
                    {
                        var tripCount = trendingLocations.First(tl => tl.LocationId == l.Id).TripCount;

                        // Parse thumbnail from images JSON string
                        string? thumbnailUrl = null;
                        if (!string.IsNullOrEmpty(l.ThumbnailUrl))
                        {
                            try
                            {
                                using var doc = System.Text.Json.JsonDocument.Parse(l.ThumbnailUrl);
                                var root = doc.RootElement;
                                if (root.ValueKind == System.Text.Json.JsonValueKind.Array && root.GetArrayLength() > 0)
                                {
                                    var first = root[0];
                                    if (first.ValueKind == System.Text.Json.JsonValueKind.Object && first.TryGetProperty("url", out var urlProp))
                                        thumbnailUrl = urlProp.GetString();
                                    else if (first.ValueKind == System.Text.Json.JsonValueKind.String)
                                        thumbnailUrl = first.GetString();
                                }
                            }
                            catch
                            {
                                thumbnailUrl = null;
                            }
                        }

                        return new TrendingLocationDto
                        {
                            Id = l.Id,
                            Name = l.Name,
                            Description = l.Description,
                            Address = l.Address,
                            Latitude = l.Latitude,
                            Longitude = l.Longitude,
                            CategoryId = l.CategoryId,
                            CategoryName = l.CategoryName,
                            CategoryIconUrl = l.CategoryIconUrl,
                            RatingAverage = l.RatingAverage,
                            RatingCount = l.RatingCount,
                            ThumbnailUrl = thumbnailUrl,
                            TripCount = tripCount
                        };
                    })
                    .OrderByDescending(l => l.TripCount)
                    .ToList();

                    result.Add(new TrendingDestinationDto
                    {
                        CityId = city.CityId,
                        CityName = city.CityName,
                        TripCount = city.TripCount,
                        TrendingLocations = locationDtos
                    });
                }

                // Create paginated result
                var paginatedResult = new PaginatedList<TrendingDestinationDto>(
                    result,
                    totalCount,
                    request.PageNumber,
                    request.PageSize);

                // Cache the result for 1 hour
                try
                {
                    await _cacheService.SetAsync(
                        cacheKey,
                        paginatedResult,
                        CacheDuration,
                        cancellationToken);
                }
                catch (Exception cacheEx)
                {
                    _logger.LogWarning(cacheEx, "Failed to cache trending destinations, continuing without cache");
                }

                _logger.LogInformation("Retrieved {Count} trending destinations (Page {Page}/{TotalPages})",
                    result.Count, request.PageNumber, paginatedResult.TotalPages);
                return Result<PaginatedList<TrendingDestinationDto>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trending destinations");
                return Result<PaginatedList<TrendingDestinationDto>>.Failure(
                    DomainErrors.Destinations.Error);
            }
        }
    }
}