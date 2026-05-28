using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;

namespace Vivu.Infrastructure.Repository
{
    public class BlogLikeRepository : GenericRepository<BlogLike>, IBlogLikeRepository
    {
        public BlogLikeRepository(VivuDbContext context) : base(context)
        {
        }

        public async Task<BlogLike?> GetBlogLikeAsync(Guid blogId, Guid userId, CancellationToken cancellationToken)
        {
            return await _context.Set<BlogLike>()
                .FirstOrDefaultAsync(l => l.BlogId == blogId && l.UserId == userId, cancellationToken);
        }

        public async Task<HashSet<Guid>> GetLikedBlogIdsAsync(Guid userId, IEnumerable<Guid> blogIds, CancellationToken cancellationToken)
        {
            var ids = await _context.Set<BlogLike>()
                .Where(l => l.UserId == userId && blogIds.Contains(l.BlogId))
                .Select(l => l.BlogId)
                .ToListAsync(cancellationToken);
            return new HashSet<Guid>(ids);
        }
    }
}
