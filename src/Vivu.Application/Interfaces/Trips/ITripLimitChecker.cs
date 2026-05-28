namespace Vivu.Application.Interfaces.Trips
{
    public interface ITripLimitChecker
    {
        Task<bool> CanCreateTripAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<int> GetTripCountAsync(Guid userId, CancellationToken cancellationToken = default);
        int GetTripLimit(bool isPremium);
    }
}
