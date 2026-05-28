using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Domain.Entities;

namespace Vivu.Domain.Interfaces
{
    public interface IChatMessageRepository : IGenericRepository<ChatMessage>
    {
        IQueryable<ChatMessage> GetByTripId(Guid tripId, CancellationToken cancellationToken);
        Task<ChatMessage?> GetByIdWithSenderAsync(Guid id, CancellationToken cancellationToken);
        Task<List<ChatMessage>> GetRecentMessagesAsync(Guid tripId, int limit, CancellationToken cancellationToken);
    }
}
