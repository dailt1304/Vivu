using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Domain.Entities;

namespace Vivu.Domain.Interfaces
{
    public interface IBlogRepository : IGenericRepository<Blog>
    {
        Task<bool> IsSlugExistsAsync(string slug, CancellationToken cancellationToken, Guid? excludeBlogId = null);
        Task<Blog?> GetBlogWithDetailsAsync(Guid blogId, CancellationToken cancellationToken);
        IQueryable<Blog> GetPublicBlogsQuery(bool includeNonPublic = false, string? filterStatus = null);
        IQueryable<Blog> GetBlogsByUserIdQuery(Guid userId);
        Task<Blog?> GetBlogDetailByIdOrSlugAsync(string idOrSlug, CancellationToken cancellationToken);

        // Full-text search
        IQueryable<Blog> GetSearchTermBlogs(string tsQueryString);

        // Statistics
        Task<int> GetTotalCountAsync(CancellationToken ct = default);
        Task<int> GetCountByStatusAsync(string status, CancellationToken ct = default);
    }
}
