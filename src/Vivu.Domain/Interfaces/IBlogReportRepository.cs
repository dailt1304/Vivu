using Vivu.Domain.Entities;

namespace Vivu.Domain.Interfaces;

public interface IBlogReportRepository : IGenericRepository<BlogReport>
{
    Task<int> GetUserReportCountTodayAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasUserReportedBlogAsync(Guid userId, Guid blogId, CancellationToken cancellationToken = default);
    IQueryable<BlogReport> GetBlogReportsQuery(string? status, string? reportType);
    Task<BlogReport?> GetReportByIdWithDetailsAsync(Guid reportId, CancellationToken cancellationToken = default);

    // Statistics
    Task<int> GetPendingReportCountAsync(CancellationToken ct = default);
}
