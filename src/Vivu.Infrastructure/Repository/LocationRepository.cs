using System.Threading;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NpgsqlTypes;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Locations;
using Vivu.Application.UseCases.Locations.Queries.GetLocationBySearchText;
using Vivu.Application.UseCases.Locations.Queries.GetLocationsWithFilters;
using Vivu.Application.UseCases.Locations.Queries.GetNearbyLocations;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace Vivu.Infrastructure.Repository
{
    public class LocationRepository : GenericRepository<Domain.Entities.Location>, ILocationRepository, IFilterLocation
    {
        public LocationRepository(VivuDbContext context) : base(context)
        {
        }

        public IQueryable<Domain.Entities.Location> GetAllLocationsQuery()
        {
            return _dbSet.AsNoTracking()
                .Where(l => !l.IsDeleted)
                .Include(l => l.City)
                    .ThenInclude(c => c.Country)
                .Include(l => l.Category)
                .Include(l => l.LocationDetail);
        }
        public async Task<List<Domain.Entities.Location>> GetAllLocationByCity(Guid CityId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Category)
                .Where(l => l.CityId == CityId && !l.IsDeleted).ToListAsync();
        }

        public override async Task<Domain.Entities.Location?> GetByIdAsync(Guid id)
        {
            return await _dbSet.AsNoTracking()
                .Include(l => l.City)
                .Include(l => l.Category)
                .Include(l => l.LocationDetail)
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
        }

        public async Task<List<Guid>> ValidateAllLocationId(List<Guid> locationguids, CancellationToken cancellationToken)
        {
            return await _dbSet
                .Where(l => locationguids.Contains(l.Id) && !l.IsDeleted)
                .Select(l => l.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<Domain.Entities.Location?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(l => l.City)
                .Include(l => l.Category)
                .Include(l => l.LocationDetail)
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted, cancellationToken);
        }

        public async Task<bool> ExistsByNameAndAddressAsync(string name, string address, Guid? excludeLocationId = null, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(l => 
                    !l.IsDeleted &&
                    l.Name.ToLower() == name.ToLower() && 
                    l.Address != null && 
                    l.Address.ToLower() == address.ToLower() &&
                    (!excludeLocationId.HasValue || l.Id != excludeLocationId.Value), 
                    cancellationToken);
        }

        public async Task<bool> ExistsByNameAndCoordinatesAsync(string name, double latitude, double longitude, Guid? excludeLocationId = null, CancellationToken cancellationToken = default)
        {
            const double tolerance = 0.001;

            return await _dbSet
                .AnyAsync(l =>
                    !l.IsDeleted &&
                    l.Name.ToLower() == name.ToLower() &&
                    l.Latitude.HasValue &&
                    l.Longitude.HasValue &&
                    Math.Abs(l.Latitude.Value - latitude) < tolerance &&
                    Math.Abs(l.Longitude.Value - longitude) < tolerance &&
                    (!excludeLocationId.HasValue || l.Id != excludeLocationId.Value),
                    cancellationToken);
        }

        public IQueryable<Domain.Entities.Location> ApplyFiltersToGetLocation(IQueryable<Domain.Entities.Location> query, GetLocationsByFilterQuery request)
        {
            if (request.IsVerifiedOnly)
            {
                query = query.Where(l => l.IsVerified);
            }

            if (request.CityId.HasValue)
            {
                query = query.Where(l => l.CityId == request.CityId.Value);
            }

            if (request.CategoryId.HasValue)
            {
                query = query.Where(l => l.CategoryId == request.CategoryId.Value);
            }

            if (request.MinRating.HasValue)
            {
                query = query.Where(l => l.RatingAverage >= request.MinRating.Value);
            }

            if (request.UserLatitude.HasValue &&
                request.UserLongitude.HasValue &&
                request.RadiusInMeters.HasValue)
            {
                GeometryFactory _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
                var userPoint = _geometryFactory.CreatePoint(
                    new Coordinate(request.UserLongitude.Value, request.UserLatitude.Value));

                query = query.Where(l =>
                    l.LocationPoint != null &&
                    l.LocationPoint.IsWithinDistance(userPoint, request.RadiusInMeters.Value));
            }

            if (!string.IsNullOrWhiteSpace(request.SearchText))
            {
                var searchTerm = $"%{request.SearchText.Trim()}%";
                query = query.Where(l =>
                    EF.Functions.ILike(l.Name, searchTerm) ||
                    (l.Address != null && EF.Functions.ILike(l.Address, searchTerm)));
            }

            return query;
        }
        public IQueryable<Domain.Entities.Location> GetUserReportedLocationsQuery(Guid userId, ReportStatus? status = null)
        {
            var query = _dbSet.AsNoTracking()
                .Include(l => l.City)
                .ThenInclude(c => c.Country)
                .Include(l => l.Category)
                .Include(l => l.LocationDetail)
                .Include(l => l.LocationReports)
                .Where(l => l.LocationReports.Any(r => r.UserId == userId) && !l.IsDeleted);

            if (status.HasValue)
            {
                var statusString = status.Value.ToString();
                query = query.Where(l => l.LocationReports.Any(r => r.UserId == userId && r.Status == statusString));
            }

            return query;
        }

        public IQueryable<Domain.Entities.Location> GetPendingLocationsQuery()
        {
            var pendingStatus = ReportStatus.PENDING.ToString();
            var newLocationType = ReportType.NEW_LOCATION.ToString();

            return _dbSet.AsNoTracking()
                .Include(l => l.City)
                .ThenInclude(c => c.Country)
                .Include(l => l.Category)
                .Include(l => l.LocationDetail)
                .Include(l => l.LocationReports.Where(r => r.Status == pendingStatus && r.ReportType == newLocationType))
                .ThenInclude(r => r.User)
                .ThenInclude(u => u.UserProfile)
                .Where(l => l.LocationReports.Any(r => r.Status == pendingStatus && r.ReportType == newLocationType) && !l.IsDeleted)
                .OrderByDescending(l => l.CreatedDate);
        }


        public IQueryable<LocationDto> ApplySorting(IQueryable<LocationDto> query, GetLocationsByFilterQuery request)
        {
            var sortBy = request.SortBy?.ToLower() ?? "rating";

            return sortBy switch
            {
                "distance" when request.UserLatitude.HasValue && request.UserLongitude.HasValue =>
                    request.IsDescending
                        ? query.OrderByDescending(l => l.DistanceInMeters)
                        : query.OrderBy(l => l.DistanceInMeters),

                "rating" =>
                    request.IsDescending
                        ? query.OrderByDescending(l => l.RatingAverage)
                              .ThenByDescending(l => l.RatingCount)
                        : query.OrderBy(l => l.RatingAverage)
                              .ThenBy(l => l.RatingCount),

                "recent" =>
                    request.IsDescending
                        ? query.OrderByDescending(l => l.CreatedAt)
                        : query.OrderBy(l => l.CreatedAt),

                "name" =>
                    request.IsDescending
                        ? query.OrderByDescending(l => l.Name)
                        : query.OrderBy(l => l.Name),

                _ =>
                    query.OrderByDescending(l => l.RatingAverage)
                         .ThenByDescending(l => l.RatingCount)
            };
        }

        public async Task<List<Domain.Entities.Location>> GetNearbyLocationsAsync(Point locationPoint, Guid excludeLocationId, double NearbyRadiusInMeters, CancellationToken cancellationToken)
        {
            var nearbyQuery = await _dbSet
                .AsNoTracking()
                .Include(l => l.Category)
                .Where(l => l.Id != excludeLocationId &&
                           l.IsVerified &&
                           l.LocationPoint != null &&
                           l.LocationPoint.IsWithinDistance(locationPoint, NearbyRadiusInMeters))
                .OrderBy(l => l.LocationPoint!.Distance(locationPoint))
                .Take(10) 
                .ToListAsync(cancellationToken);

            return nearbyQuery;
        }

        // Statistics methods
        public async Task<Dictionary<string, int>> GetLocationsByCategoryAsync(CancellationToken cancellationToken = default)
        {
            var result = await _dbSet
                .Where(l => !l.IsDeleted)
                .GroupBy(l => l.Category!.Name)
                .Select(g => new { CategoryName = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return result.ToDictionary(x => x.CategoryName, x => x.Count);
        }

        public async Task<int> GetPendingSubmissionsCountAsync(CancellationToken cancellationToken = default)
        {
            var reportTypeString = ReportType.NEW_LOCATION.ToString();
            var statusString = ReportStatus.PENDING.ToString();
            
            return await _dbSet
                .Where(l => !l.IsDeleted && 
                           l.LocationReports.Any(r => 
                               r.ReportType == reportTypeString && 
                               r.Status == statusString))
                .CountAsync(cancellationToken);
        }

        public async Task<int> GetTotalVerifiedLocationsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(l => !l.IsDeleted && l.IsVerified)
                .CountAsync(cancellationToken);
        }

        public async Task<decimal> GetVerificationRateAsync(CancellationToken cancellationToken = default)
        {
            var totalLocations = await _dbSet
                .Where(l => !l.IsDeleted)
                .CountAsync(cancellationToken);

            if (totalLocations == 0)
            {
                return 0;
            }

            var verifiedLocations = await _dbSet
                .Where(l => !l.IsDeleted && l.IsVerified)
                .CountAsync(cancellationToken);

            return Math.Round((decimal)verifiedLocations / totalLocations * 100, 2);
        }

        public IQueryable<Domain.Entities.Location> ApplyFiltersToGetSearchLocation(IQueryable<Domain.Entities.Location> query, SearchLocationsQuery request)
        {
            if (request.IsVerifiedOnly)
            {
                query = query.Where(l => l.IsVerified);
            }

            if (request.CityId.HasValue)
            {
                query = query.Where(l => l.CityId == request.CityId.Value);
            }

            if (request.CategoryId.HasValue)
            {
                query = query.Where(l => l.CategoryId == request.CategoryId.Value);
            }

            if (request.MinRating.HasValue)
            {
                query = query.Where(l => l.RatingAverage >= request.MinRating.Value);
            }
            return query;
        }

        public IQueryable<Domain.Entities.Location> GetSearchTermLocation(string tsQueryString)
        {
            return _dbSet
                .AsNoTracking()
                .Include(l => l.City)
                .Include(l => l.Category)
                .Include(l => l.LocationDetail)
                .Where(l => !l.IsDeleted && EF.Property<NpgsqlTsVector>(l, "SearchVector")
                .Matches(EF.Functions.ToTsQuery("simple", tsQueryString)));
        }

        public IQueryable<Domain.Entities.Location> GetVerifyLocations()
        {
            return _dbSet.AsNoTracking()
                .Include(l => l.City)
                .Include(l => l.Category)
                .Include(l => l.LocationDetail)
                .Where(l => !l.IsDeleted && l.IsVerified);
        }

        public async Task<List<Domain.Entities.Location>> GetPopularLocations(IQueryable<Domain.Entities.Location> query, int limit, CancellationToken cancellationToken)
        {
            return await query
                .OrderByDescending(l => l.RatingAverage)
                .ThenByDescending(l => l.RatingCount)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        public IQueryable<Domain.Entities.Location> GetPointLocation(int limit, Point userPoint)
        {
            return _dbSet.AsNoTracking()
                .Include(l => l.City)
                .Include(l => l.Category)
                .Include(l => l.LocationDetail)
                .Where(l => !l.IsDeleted && l.LocationPoint != null)
                .OrderBy(l => l.LocationPoint!.Distance(userPoint))
                .Take(limit);
        }

        public IQueryable<Domain.Entities.Location> ApplyFiltersToGetNearbyLocation(IQueryable<Domain.Entities.Location> query, GetNearbyLocationsQuery request)
        {
            if (request.IsVerifiedOnly)
            {
                query = query.Where(l => l.IsVerified);
            }

            if (request.CategoryId.HasValue)
            {
                query = query.Where(l => l.CategoryId == request.CategoryId.Value);
            }

            if (request.MinRating.HasValue)
            {
                query = query.Where(l => l.RatingAverage >= request.MinRating.Value);
            }

            return query;
        }
    }
}
