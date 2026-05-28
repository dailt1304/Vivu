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
    public class UserSubscriptionRepository : GenericRepository<UserSubscription>, IUserSubscriptionRepository
    {
        public UserSubscriptionRepository(VivuDbContext context) : base(context) { }
        public async Task<UserSubscription?> GetUserActiveSubscription(Guid userId, CancellationToken cancellationToken)
        {
            var subscription = await _dbSet.AsNoTracking()
                    .Include(s => s.Package)
                    .Where(s => s.UserId == userId &&
                               s.Status == "Active" &&
                               s.StartDate <= DateTime.UtcNow &&
                               s.EndDate >= DateTime.UtcNow)
                    .OrderByDescending(s => s.EndDate)
                    .FirstOrDefaultAsync(cancellationToken);
            return subscription;
        }

        public async Task<List<UserSubscription>> GetExpiredActiveSubscriptionsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.User)
                    .ThenInclude(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                .Where(s => s.Status == "Active" && s.EndDate < DateTime.UtcNow)
                .ToListAsync(cancellationToken);
        }

        // Statistics
        public async Task<int> GetActiveSubscriptionsCountAsync(CancellationToken ct = default)
            => await _dbSet.CountAsync(s => s.Status == "Active" && s.EndDate >= DateTime.UtcNow, ct);

        public async Task<int> GetExpiringSubscriptionsCountAsync(int withinDays, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var cutoff = now.AddDays(withinDays);
            return await _dbSet.CountAsync(s =>
                s.Status == "Active" && s.EndDate <= cutoff && s.EndDate > now, ct);
        }
    }
}
