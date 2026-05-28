using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;

namespace Vivu.Infrastructure.Repository
{
    public class BlogPostTagRepository : GenericRepository<BlogPostTag>, IBlogPostTagRepository
    {
        public BlogPostTagRepository(VivuDbContext context) : base(context) { }
        
    }
}
