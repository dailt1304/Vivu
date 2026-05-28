using Microsoft.EntityFrameworkCore;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;

namespace Vivu.Infrastructure.Repository;

public class LocationReportRepository : GenericRepository<LocationReport>, ILocationReportRepository
{
    public LocationReportRepository(VivuDbContext context) : base(context)
    {
    }

    public async Task<int> GetUserReportCountTodayAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        return await _dbSet
            .Where(r => r.UserId == userId && r.CreatedDate >= today && r.CreatedDate < tomorrow)
            .CountAsync(cancellationToken);
    }

    public async Task<bool> HasUserPendingReportForLocationAsync(Guid userId, Guid locationId, CancellationToken cancellationToken = default)
    {
        var pendingStatus = ReportStatus.PENDING.ToString();
        return await _dbSet
            .AnyAsync(r => r.UserId == userId && r.LocationId == locationId && r.Status == pendingStatus, cancellationToken);
    }

    public IQueryable<LocationReport> GetLocationReportsQuery(string? status, string? reportType)
    {
        var query = _dbSet
            .Include(r => r.Location)
            .Include(r => r.User)
                .ThenInclude(u => u.UserProfile)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(r => r.Status.ToUpper() == status.ToUpper());
        }

        if (!string.IsNullOrEmpty(reportType))
        {
            query = query.Where(r => r.ReportType.ToUpper() == reportType.ToUpper());
        }

        return query.OrderByDescending(r => r.CreatedDate);
    }

    public async Task<LocationReport?> GetReportByIdWithDetailsAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Location)
            .Include(r => r.User)
                .ThenInclude(u => u.UserProfile)
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
    }

    public async Task<int> GetPendingReportCountForLocationAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.LocationId == locationId && r.Status == "PENDING")
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetClosedReportCountForLocationAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.LocationId == locationId && r.ReportType == ReportType.CLOSED.ToString())
            .CountAsync(cancellationToken);
    }

    public async Task<bool> ExistsPendingNewLocationSubmissionAsync(Guid userId, string name, string? address, double? latitude, double? longitude, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(r =>r.UserId == userId && r.Status == ReportStatus.PENDING.ToString() && r.ReportType == ReportType.NEW_LOCATION.ToString());

        // Join with Location to check name and address/coordinates
        var hasPending = await query
            .Join(
                _context.Set<Location>(),
                report => report.LocationId,
                location => location.Id,
                (report, location) => new { report, location })
            .AnyAsync(x =>
                x.location.Name.ToLower() == name.ToLower() &&
                (
                    // Match by address if provided
                    (!string.IsNullOrWhiteSpace(address) &&
                     x.location.Address != null &&
                     x.location.Address.ToLower() == address.ToLower()) ||
                    // Match by coordinates if provided (with tolerance)
                    (latitude.HasValue && longitude.HasValue &&
                     x.location.Latitude.HasValue && x.location.Longitude.HasValue &&
                     Math.Abs(x.location.Latitude.Value - latitude.Value) < 0.001 &&
                     Math.Abs(x.location.Longitude.Value - longitude.Value) < 0.001)
                ),
                cancellationToken);

        return hasPending;
    }

    // Statistics methods
    public async Task<Dictionary<string, int>> GetReportsByTypeAsync(CancellationToken cancellationToken = default)
    {
        var result = await _dbSet
            .GroupBy(r => r.ReportType)
            .Select(g => new { ReportType = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return result.ToDictionary(x => x.ReportType, x => x.Count);
    }
}
