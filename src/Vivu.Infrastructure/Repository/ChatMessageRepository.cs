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
    public class ChatMessageRepository : GenericRepository<ChatMessage>, IChatMessageRepository
    {
        public ChatMessageRepository(VivuDbContext context) : base(context) { }

        public IQueryable<ChatMessage> GetByTripId(Guid tripId, CancellationToken cancellationToken)
        {
            return  _dbSet.AsNoTracking()
                    .Include(m => m.Sender)
                        .ThenInclude(u => u.UserProfile)
                    .Include(m => m.Files)
                    .Where(m => m.TripId == tripId && !m.IsDeleted)
                    .OrderByDescending(m => m.CreatedDate);
        }

        public async Task<ChatMessage?> GetByIdWithSenderAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _dbSet.AsNoTracking()
                .Include(m => m.Sender)
                    .ThenInclude(u => u.UserProfile)
                .Include(m => m.Files)
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }
        public async Task<List<ChatMessage>> GetRecentMessagesAsync(
            Guid tripId, int limit, CancellationToken cancellationToken)
        {
            return await _dbSet.AsNoTracking()
                .Where(m => m.TripId == tripId && !m.IsDeleted
                    && !(m.IsAiMessage && m.MessageType == "trip_plan")) 
                .OrderByDescending(m => m.CreatedDate)
                .Take(limit)
                .OrderBy(m => m.CreatedDate) 
                .ToListAsync(cancellationToken);
        }
    }
}
