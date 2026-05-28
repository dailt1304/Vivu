using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Domain.Entities;

namespace Vivu.Domain.Interfaces
{
    public interface IUserSubscriptionRepository : IGenericRepository<UserSubscription>
    {
        Task<UserSubscription?> GetUserActiveSubscription(Guid userId, CancellationToken cancellationToken);
        Task<List<UserSubscription>> GetExpiredActiveSubscriptionsAsync(CancellationToken cancellationToken = default);

        // Statistics
        Task<int> GetActiveSubscriptionsCountAsync(CancellationToken ct = default);
        Task<int> GetExpiringSubscriptionsCountAsync(int withinDays, CancellationToken ct = default);
    }
}
