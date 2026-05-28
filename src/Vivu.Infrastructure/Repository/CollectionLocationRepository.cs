using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;

namespace Vivu.Infrastructure.Repository
{
    public class CollectionLocationRepository : GenericRepository<CollectionLocation>, ICollectionLocationRepository
    {
        public CollectionLocationRepository(VivuDbContext context) : base(context)
        {
        }

        public async Task<CollectionLocation?> GetAsync(Guid collectionId, Guid locationId, CancellationToken ct)
        {
            return await _dbSet.FirstOrDefaultAsync(cl => cl.CollectionId == collectionId && cl.LocationId == locationId, ct);
        }

        public async Task<List<CollectionLocation>> GetByCollectionId(Guid collectionId, CancellationToken ct)
        {
            return await _dbSet.Where(cl => cl.CollectionId == collectionId).ToListAsync(ct);
        }

        public void RemoveRange(IEnumerable<CollectionLocation> items)
        {
            _dbSet.RemoveRange(items);
        }
    }
}
