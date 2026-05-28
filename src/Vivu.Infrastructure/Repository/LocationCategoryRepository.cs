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
    public class LocationCategoryRepository : GenericRepository<LocationCategory>, ILocationCategoryRepository
    {
        public LocationCategoryRepository(VivuDbContext context) : base(context)
        {
        }

        public IQueryable<LocationCategory> GetAllCategoriesWithLocationCountQuery()
        {
            return _dbSet
                .AsNoTracking()
                .Include(c => c.Locations);
        }
        public async Task<List<string>> GetAllCategoryNamesAsync()
        {
            return await _dbSet.Select(lc => lc.Name)
                .ToListAsync();
        }
    }
}
