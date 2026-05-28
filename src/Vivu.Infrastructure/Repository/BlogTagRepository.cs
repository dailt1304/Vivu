using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;

namespace Vivu.Infrastructure.Repository
{
    public class BlogTagRepository : GenericRepository<BlogTag>, IBlogTagRepository
    {
        public BlogTagRepository(VivuDbContext context) : base(context) { }
        public async Task<BlogTag?> GetByNameAsync(string name, CancellationToken cancellationToken)
        {
            return await _dbSet.FirstOrDefaultAsync(t => t.Name == name, cancellationToken);
        }

    }
}
