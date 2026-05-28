using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vivu.Domain.Entities;

namespace Vivu.Domain.Interfaces
{
    public interface ITripMemberRepository : IGenericRepository<TripMember>
    {
        Task<Guid?> GetOwnerIdByTripId(Guid TripId);
        Task<TripMember?> GetByTripAndUserAsync(Guid tripId, Guid userId, CancellationToken cancellationToken);
        Task<List<TripMember>> GetMembersByTripIdWithUserProfileAsync(Guid tripId, CancellationToken cancellationToken);
        IQueryable<TripMember> GetMembersByTripIdQuery(Guid tripId);
        Task<List<Guid>> GetUserTripIdsAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
