using Microsoft.EntityFrameworkCore;
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
    public class TripMemberRepository : GenericRepository<TripMember>, ITripMemberRepository
    {
        public TripMemberRepository(VivuDbContext context) : base(context)
        {
        }

        public async Task<TripMember?> GetByTripAndUserAsync(Guid tripId, Guid userId, CancellationToken cancellationToken)
        {
            return await _dbSet
            .Include(tm => tm.User)
            .ThenInclude(u => u.UserProfile)
            .Where(tm => tm.TripId == tripId && tm.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Guid?> GetOwnerIdByTripId(Guid TripId)
        {
            var ownerId = await _dbSet
                .Where(tm => tm.TripId == TripId)
                .Select(tm => (Guid?)tm.OwnerId)
                .FirstOrDefaultAsync();
            return ownerId;
        }

        public async Task<List<Guid>> GetUserTripIdsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking()
                .Where(m => m.UserId == userId)
                .Select(m => m.TripId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<TripMember>> GetMembersByTripIdWithUserProfileAsync(Guid tripId, CancellationToken cancellationToken)
        {
            return await _dbSet
                .Where(tm => tm.TripId == tripId)
                .Include(tm => tm.User)
                    .ThenInclude(u => u.UserProfile)
                .Include(tm => tm.Trip)
                .OrderBy(tm => tm.Role == "owner" ? 0 : 1)
                .ThenBy(tm => tm.JoinedAt)
                .ToListAsync(cancellationToken);
        }

        public IQueryable<TripMember> GetMembersByTripIdQuery(Guid tripId)
        {
            return _dbSet
                .Where(tm => tm.TripId == tripId)
                .Include(tm => tm.User)
                    .ThenInclude(u => u.UserProfile)
                .Include(tm => tm.Trip)
                .OrderBy(tm => tm.Role == "owner" ? 0 : 1)
                .ThenBy(tm => tm.JoinedAt);
        }
    }
}
