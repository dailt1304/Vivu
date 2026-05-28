using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using NpgsqlTypes;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;

namespace Vivu.Domain.Interfaces
{
    public interface ILocationRepository : IGenericRepository<Entities.Location>
    {
        IQueryable<Entities.Location> GetAllLocationsQuery();
        IQueryable<Entities.Location> GetPointLocation(int limit, Point userPoint);
        Task<List<Entities.Location>> GetAllLocationByCity(Guid CityId);
        Task<List<Entities.Location>> GetPopularLocations(IQueryable<Domain.Entities.Location> query, int limit, CancellationToken cancellationToken);
        IQueryable<Domain.Entities.Location> GetVerifyLocations();
        IQueryable<Entities.Location> GetSearchTermLocation(string tsQueryString);
        Task<List<Entities.Location>> GetNearbyLocationsAsync(
            Point locationPoint,
            Guid excludeLocationId,
            double NearbyRadiusInMeters,
            CancellationToken cancellationToken);
        Task<List<Guid>> ValidateAllLocationId(List<Guid> locationguids, CancellationToken cancellationToken);
        Task<Entities.Location?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsByNameAndAddressAsync(string name, string address, Guid? excludeLocationId = null, CancellationToken cancellationToken = default);
        Task<bool> ExistsByNameAndCoordinatesAsync(string name, double latitude, double longitude, Guid? excludeLocationId = null, CancellationToken cancellationToken = default);
        IQueryable<Entities.Location> GetUserReportedLocationsQuery(Guid userId, ReportStatus? status = null);
        IQueryable<Entities.Location> GetPendingLocationsQuery();
        
        // Statistics methods
        Task<Dictionary<string, int>> GetLocationsByCategoryAsync(CancellationToken cancellationToken = default);
        Task<int> GetPendingSubmissionsCountAsync(CancellationToken cancellationToken = default);
        Task<decimal> GetVerificationRateAsync(CancellationToken cancellationToken = default);
        Task<int> GetTotalVerifiedLocationsAsync(CancellationToken cancellationToken = default);
    }
}
