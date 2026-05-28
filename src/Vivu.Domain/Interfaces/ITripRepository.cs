using System.Threading;
using Vivu.Domain.AI;
using Vivu.Domain.Entities;

namespace Vivu.Domain.Interfaces;

public interface ITripRepository : IGenericRepository<Trip>
{
    IQueryable<Trip> GetTripsByUserIdQuery(Guid userId);
    IQueryable<Trip> GetPublicTripsQuery();
    Task<Trip?> GetTripWithMembersAsync(Guid tripId, CancellationToken cancellationToken = default);
    Task<Trip?> GetTripByIdWithDetailsAsync(Guid tripId, CancellationToken cancellationToken = default);
    Task<Trip?> GetTripWithRatingAsync(Guid tripId, CancellationToken cancellationToken = default);
    Task<List<Trip>> GetTripsForStatusUpdateAsync(CancellationToken cancellationToken = default);
    Task<bool> CheckAvailableInviteCode(string code, CancellationToken cancellationToken);
    Task<Trip?> GetByInviteCodeAsync(string InviteCode, CancellationToken cancellationToken);
    IQueryable<Trip> GetSearchTermTrips(string tsQueryString);
    Task<Trip?> GetTripByIdWithDetailsForAIModifyAsync(Guid tripId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a lightweight history of ALL non-deleted trips owned by <paramref name="userId"/>:
    /// city info + up to 10 distinct visited location names per trip.
    /// Used by <see cref="IUserPersonalizationService"/> to build exclusion lists.
    /// </summary>
    Task<List<TripHistorySummary>> GetUserTripHistoryAsync(Guid userId, CancellationToken ct = default);

    // Statistics
    Task<int> GetTotalCountAsync(CancellationToken ct = default);
}
