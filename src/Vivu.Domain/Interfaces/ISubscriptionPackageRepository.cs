using Vivu.Domain.Entities;

namespace Vivu.Domain.Interfaces;

public interface ISubscriptionPackageRepository : IGenericRepository<SubscriptionPackage>
{
    IQueryable<SubscriptionPackage> GetAllQuery();
    Task<SubscriptionPackage?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> IsCodeExistsAsync(string code, CancellationToken cancellationToken = default);
    Task<List<SubscriptionPackage>> GetActivePackagesAsync(CancellationToken cancellationToken = default);
}
