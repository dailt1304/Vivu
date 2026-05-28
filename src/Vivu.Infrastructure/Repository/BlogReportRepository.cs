using Microsoft.EntityFrameworkCore;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;

namespace Vivu.Infrastructure.Repository;

public class BlogReportRepository : GenericRepository<BlogReport>, IBlogReportRepository
{
    public BlogReportRepository(VivuDbContext context) : base(context)
    {
    }

    public async Task<int> GetUserReportCountTodayAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        return await _dbSet
            .Where(r => r.ReporterId == userId && r.CreatedDate >= today && r.CreatedDate < tomorrow)
            .CountAsync(cancellationToken);
    }

    public async Task<bool> HasUserReportedBlogAsync(Guid userId, Guid blogId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(r => r.ReporterId == userId && r.BlogId == blogId, cancellationToken);
    }

    public IQueryable<BlogReport> GetBlogReportsQuery(string? status, string? reportType)
    {
        var query = _dbSet
            .Include(r => r.Blog)
            .Include(r => r.Reporter)
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

    public async Task<BlogReport?> GetReportByIdWithDetailsAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Blog)
            .Include(r => r.Reporter)
                .ThenInclude(u => u.UserProfile)
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
    }

    // Statistics
    public async Task<int> GetPendingReportCountAsync(CancellationToken ct = default)
        => await _dbSet.CountAsync(r => r.Status == "PENDING", ct);
}
