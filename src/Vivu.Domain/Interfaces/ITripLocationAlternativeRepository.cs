using Vivu.Domain.Entities;

namespace Vivu.Domain.Interfaces
{
    public interface ITripLocationAlternativeRepository : IGenericRepository<TripLocationAlternative>
    {
        Task<List<TripLocationAlternative>> GetByTripLocationIdAsync(
            Guid tripLocationId, CancellationToken ct = default);

        Task<int> CountByTripLocationIdAsync(
            Guid tripLocationId, CancellationToken ct = default);

        Task<bool> ExistsAsync(
            Guid tripLocationId, Guid locationId, CancellationToken ct = default);
    }
}
