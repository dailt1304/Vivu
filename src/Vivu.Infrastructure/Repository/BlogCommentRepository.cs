using Microsoft.EntityFrameworkCore;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;

namespace Vivu.Infrastructure.Repository
{
    public class BlogCommentRepository : GenericRepository<BlogComment>, IBlogCommentRepository
    {
        public BlogCommentRepository(VivuDbContext context) : base(context) { }

        public IQueryable<BlogComment> GetCommentsByBlogIdQuery(Guid blogId)
        {
            return _dbSet
                .Include(c => c.User)
                    .ThenInclude(u => u.UserProfile)
                .Where(c => c.BlogId == blogId && c.ParentCommentId == null)
                .AsQueryable();
        }
    }
}
