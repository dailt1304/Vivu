using Microsoft.EntityFrameworkCore;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;

namespace Vivu.Infrastructure.Repository
{
    public class TripLocationAlternativeRepository
        : GenericRepository<TripLocationAlternative>, ITripLocationAlternativeRepository
    {
        public TripLocationAlternativeRepository(VivuDbContext context) : base(context)
        {
        }

        public Task<List<TripLocationAlternative>> GetByTripLocationIdAsync(
            Guid tripLocationId, CancellationToken ct = default)
        {
            return _dbSet
                .Include(a => a.Location)
                    .ThenInclude(l => l.LocationDetail)
                .Where(a => a.TripLocationId == tripLocationId)
                .OrderBy(a => a.Priority)
                .ToListAsync(ct);
        }

        public Task<int> CountByTripLocationIdAsync(
            Guid tripLocationId, CancellationToken ct = default)
        {
            return _dbSet.CountAsync(a => a.TripLocationId == tripLocationId, ct);
        }

        public Task<bool> ExistsAsync(
            Guid tripLocationId, Guid locationId, CancellationToken ct = default)
        {
            return _dbSet.AnyAsync(
                a => a.TripLocationId == tripLocationId && a.LocationId == locationId, ct);
        }
    }
}
