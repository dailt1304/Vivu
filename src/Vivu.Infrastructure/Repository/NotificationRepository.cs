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
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(VivuDbContext context) : base(context) { }

        public IQueryable<Notification> GetByUserIdQuery(Guid userId, bool trackChanges = false)
        {
            var query = _dbSet.Where(n => n.UserId == userId);
            
            if (!trackChanges)
            {
                query = query.AsNoTracking();
            }

            return query.OrderByDescending(n => n.CreatedDate);
        }

        public async Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync(cancellationToken);
        }
    }
}
