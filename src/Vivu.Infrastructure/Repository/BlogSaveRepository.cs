using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;

namespace Vivu.Infrastructure.Repository
{
    public class BlogSaveRepository : GenericRepository<BlogSave>, IBlogSaveRepository
    {
        public BlogSaveRepository(VivuDbContext context) : base(context)
        {
        }

        public async Task<BlogSave?> GetBlogSaveAsync(Guid blogId, Guid userId, CancellationToken cancellationToken)
        {
            return await _context.Set<BlogSave>()
                .FirstOrDefaultAsync(s => s.BlogId == blogId && s.UserId == userId, cancellationToken);
        }

        public IQueryable<Blog> GetBookmarkedBlogsByUserIdQuery(Guid userId)
        {
            return _context.Set<BlogSave>()
                .Where(s => s.UserId == userId)
                .Include(s => s.Blog)
                    .ThenInclude(b => b.User)
                        .ThenInclude(u => u.UserProfile)
                .Select(s => s.Blog)
                .Where(b => b.Status == "published")
                .AsQueryable();
        }

        public async Task<HashSet<Guid>> GetSavedBlogIdsAsync(Guid userId, IEnumerable<Guid> blogIds, CancellationToken cancellationToken)
        {
            var ids = await _context.Set<BlogSave>()
                .Where(s => s.UserId == userId && blogIds.Contains(s.BlogId))
                .Select(s => s.BlogId)
                .ToListAsync(cancellationToken);
            return new HashSet<Guid>(ids);
        }
    }
}
