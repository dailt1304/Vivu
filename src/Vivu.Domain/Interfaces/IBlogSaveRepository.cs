using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vivu.Domain.Entities;

namespace Vivu.Domain.Interfaces
{
    public interface IBlogSaveRepository : IGenericRepository<BlogSave>
    {
        Task<BlogSave?> GetBlogSaveAsync(Guid blogId, Guid userId, CancellationToken cancellationToken);
        IQueryable<Blog> GetBookmarkedBlogsByUserIdQuery(Guid userId);
        Task<HashSet<Guid>> GetSavedBlogIdsAsync(Guid userId, IEnumerable<Guid> blogIds, CancellationToken cancellationToken);
    }
}
