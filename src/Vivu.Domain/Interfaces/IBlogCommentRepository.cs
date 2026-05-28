using Vivu.Domain.Entities;

namespace Vivu.Domain.Interfaces
{
    public interface IBlogCommentRepository : IGenericRepository<BlogComment>
    {
        IQueryable<BlogComment> GetCommentsByBlogIdQuery(Guid blogId);
    }
}
