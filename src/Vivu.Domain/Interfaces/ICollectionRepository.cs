using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vivu.Domain.Entities;

namespace Vivu.Domain.Interfaces
{
    public interface ICollectionRepository : IGenericRepository<Collection>
    {
        IQueryable<Collection> GetUserCollections(Guid userId);
        IQueryable<Collection> SearchUserCollections(Guid userId, string searchText);
        Task<Collection?> GetCollectionWithLocations(Guid collectionId, CancellationToken ct);
        Task<bool> IsNameDuplicate(Guid userId, string name, Guid? excludeId, CancellationToken ct);
        IQueryable<Collection> GetUserCollectionsSummary(Guid userId); // For dropdown selection
    }
}
