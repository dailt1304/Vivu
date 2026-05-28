using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vivu.Domain.Entities;

namespace Vivu.Domain.Interfaces
{
    public interface ICollectionLocationRepository : IGenericRepository<CollectionLocation>
    {
        Task<CollectionLocation?> GetAsync(Guid collectionId, Guid locationId, CancellationToken ct);
        Task<List<CollectionLocation>> GetByCollectionId(Guid collectionId, CancellationToken ct);
        void RemoveRange(IEnumerable<CollectionLocation> items);
    }
}
