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
    public class CollectionRepository : GenericRepository<Collection>, ICollectionRepository
    {
        public CollectionRepository(VivuDbContext context) : base(context) { }

        public IQueryable<Collection> GetUserCollections(Guid userId)
        {
            return _dbSet.AsNoTracking()
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .Include(c => c.CollectionLocations)
                    .ThenInclude(cl => cl.Location)
                        .ThenInclude(l => l.LocationDetail)
                .Include(c => c.CollectionLocations)
                    .ThenInclude(cl => cl.Location)
                        .ThenInclude(l => l.City)
                .Include(c => c.CollectionLocations)
                    .ThenInclude(cl => cl.Location)
                        .ThenInclude(l => l.Category)
                .OrderByDescending(c => c.ModifiedDate ?? c.CreatedDate);
        }

        public IQueryable<Collection> SearchUserCollections(Guid userId, string searchText)
        {
            var normalizedSearch = searchText.Trim().ToLower();
            
            return _dbSet.AsNoTracking()
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .Where(c => 
                    c.Name.ToLower().Contains(normalizedSearch) ||
                    (c.Description != null && c.Description.ToLower().Contains(normalizedSearch))
                )
                .Include(c => c.CollectionLocations)
                    .ThenInclude(cl => cl.Location)
                        .ThenInclude(l => l.LocationDetail)
                .Include(c => c.CollectionLocations)
                    .ThenInclude(cl => cl.Location)
                        .ThenInclude(l => l.City)
                .Include(c => c.CollectionLocations)
                    .ThenInclude(cl => cl.Location)
                        .ThenInclude(l => l.Category)
                .OrderByDescending(c => c.ModifiedDate ?? c.CreatedDate);
        }

        public async Task<Collection?> GetCollectionWithLocations(Guid collectionId, CancellationToken ct)
        {
            return await _dbSet
                .Where(c => c.Id == collectionId && !c.IsDeleted)
                .Include(c => c.CollectionLocations)
                    .ThenInclude(cl => cl.Location)
                        .ThenInclude(l => l.LocationDetail)
                .Include(c => c.CollectionLocations)
                    .ThenInclude(cl => cl.Location)
                        .ThenInclude(l => l.City)
                .Include(c => c.CollectionLocations)
                    .ThenInclude(cl => cl.Location)
                        .ThenInclude(l => l.Category)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<bool> IsNameDuplicate(Guid userId, string name, Guid? excludeId, CancellationToken ct)
        {
            var query = _dbSet.Where(c => c.UserId == userId && c.Name == name && !c.IsDeleted);
            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }
            return await query.AnyAsync(ct);
        }

        public IQueryable<Collection> GetUserCollectionsSummary(Guid userId)
        {
            return _dbSet.AsNoTracking()
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .Include(c => c.CollectionLocations)
                .OrderByDescending(c => c.ModifiedDate ?? c.CreatedDate);
        }
    }
}
