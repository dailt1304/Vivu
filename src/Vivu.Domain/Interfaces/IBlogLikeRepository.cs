using System;
using System.Threading;
using System.Threading.Tasks;
using Vivu.Domain.Entities;

namespace Vivu.Domain.Interfaces
{
    public interface IBlogLikeRepository : IGenericRepository<BlogLike>
    {
        Task<BlogLike?> GetBlogLikeAsync(Guid blogId, Guid userId, CancellationToken cancellationToken);
        Task<HashSet<Guid>> GetLikedBlogIdsAsync(Guid userId, IEnumerable<Guid> blogIds, CancellationToken cancellationToken);
    }
}
