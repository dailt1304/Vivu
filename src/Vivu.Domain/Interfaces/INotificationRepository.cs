using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Domain.Entities;

namespace Vivu.Domain.Interfaces
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        IQueryable<Notification> GetByUserIdQuery(Guid userId, bool trackChanges = false);
        Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
