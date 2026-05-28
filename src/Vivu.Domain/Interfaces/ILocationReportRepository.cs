using Vivu.Domain.Entities;

namespace Vivu.Domain.Interfaces;

public interface ILocationReportRepository : IGenericRepository<LocationReport>
{
    Task<int> GetUserReportCountTodayAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasUserPendingReportForLocationAsync(Guid userId, Guid locationId, CancellationToken cancellationToken = default);
    IQueryable<LocationReport> GetLocationReportsQuery(string? status, string? reportType);
    Task<LocationReport?> GetReportByIdWithDetailsAsync(Guid reportId, CancellationToken cancellationToken = default);
    Task<int> GetPendingReportCountForLocationAsync(Guid locationId, CancellationToken cancellationToken = default);
    Task<int> GetClosedReportCountForLocationAsync(Guid locationId, CancellationToken cancellationToken = default);
    Task<bool> ExistsPendingNewLocationSubmissionAsync(Guid userId, string name, string? address, double? latitude, double? longitude, CancellationToken cancellationToken = default);
    
    // Statistics methods
    Task<Dictionary<string, int>> GetReportsByTypeAsync(CancellationToken cancellationToken = default);
}
