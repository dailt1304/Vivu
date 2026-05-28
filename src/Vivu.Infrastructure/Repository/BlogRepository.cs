using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;

namespace Vivu.Infrastructure.Repository
{
    public class BlogRepository : GenericRepository<Blog>, IBlogRepository
    {
        public BlogRepository(VivuDbContext context) : base(context) { }

        public async Task<bool> IsSlugExistsAsync(string slug, CancellationToken cancellationToken, Guid? excludeBlogId = null)
        {
            return await _dbSet.AnyAsync(b => b.Slug == slug && (!excludeBlogId.HasValue || b.Id != excludeBlogId.Value),
                                            cancellationToken);
        }
        public async Task<Blog?> GetBlogWithDetailsAsync(Guid blogId, CancellationToken cancellationToken)
        {
            return await _dbSet
                .Include(b => b.BlogStoryDays)
                .Include(b => b.BlogPostTags)
                    .ThenInclude(pt => pt.Tag)
                .FirstOrDefaultAsync(b => b.Id == blogId, cancellationToken);
        }

        public IQueryable<Blog> GetPublicBlogsQuery(bool includeNonPublic = false, string? filterStatus = null)
        {
            var query = _dbSet
                .Include(b => b.User)
                    .ThenInclude(u => u.UserProfile)
                .AsQueryable();

            if (!includeNonPublic)
            {
                query = query.Where(b => b.Status == "published");
            }
            else if (!string.IsNullOrEmpty(filterStatus) && filterStatus.ToLower() != "all")
            {
                var statusLower = filterStatus.ToLower();
                query = query.Where(b => b.Status == statusLower);
            }

            return query;
        }

        public IQueryable<Blog> GetBlogsByUserIdQuery(Guid userId)
        {
            return _dbSet
                .Include(b => b.User)
                    .ThenInclude(u => u.UserProfile)
                .Where(b => b.UserId == userId)
                .AsQueryable();
        }
        
        public async Task<Blog?> GetBlogDetailByIdOrSlugAsync(string idOrSlug, CancellationToken cancellationToken)
        {
            var baseQuery = _dbSet
                .Include(b => b.User)
                    .ThenInclude(u => u.UserProfile)
                .Include(b => b.BlogStoryDays)
                .Include(b => b.BlogPostTags)
                    .ThenInclude(pt => pt.Tag)
                .Include(b => b.Trip)
                    .ThenInclude(t => t!.TripDays)
                        .ThenInclude(d => d.TripLocations)
                            .ThenInclude(tl => tl.Location)
                                .ThenInclude(l => l.Category)
                .Include(b => b.Trip)
                    .ThenInclude(t => t!.TripDays)
                        .ThenInclude(d => d.TripLocations)
                            .ThenInclude(tl => tl.Location)
                                .ThenInclude(l => l.LocationDetail);

            if (Guid.TryParse(idOrSlug, out var blogId))
            {
                return await baseQuery.FirstOrDefaultAsync(b => b.Id == blogId, cancellationToken);
            }

            return await baseQuery.FirstOrDefaultAsync(b => b.Slug == idOrSlug, cancellationToken);
        }

        public IQueryable<Blog> GetSearchTermBlogs(string tsQueryString)
        {
            return _dbSet
                .AsNoTracking()
                .Where(b => b.Status == "published")
                .Include(b => b.User)
                    .ThenInclude(u => u.UserProfile)
                .Where(b => EF.Property<NpgsqlTsVector>(b, "SearchVector")
                    .Matches(EF.Functions.ToTsQuery("simple", tsQueryString)));
        }

        // Statistics
        public async Task<int> GetTotalCountAsync(CancellationToken ct = default)
            => await _dbSet.CountAsync(ct);

        public async Task<int> GetCountByStatusAsync(string status, CancellationToken ct = default)
            => await _dbSet.CountAsync(b => b.Status == status, ct);
    }
}

