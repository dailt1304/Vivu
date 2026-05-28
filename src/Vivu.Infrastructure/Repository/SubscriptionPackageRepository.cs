using Microsoft.EntityFrameworkCore;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;

namespace Vivu.Infrastructure.Repository;

public class SubscriptionPackageRepository : GenericRepository<SubscriptionPackage>, ISubscriptionPackageRepository
{
    public SubscriptionPackageRepository(VivuDbContext context) : base(context) { }

    public IQueryable<SubscriptionPackage> GetAllQuery()
    {
        return _dbSet.AsNoTracking();
    }

    public async Task<SubscriptionPackage?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(sp => sp.Code == code, cancellationToken);
    }

    public async Task<bool> IsCodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .AnyAsync(sp => sp.Code == code, cancellationToken);
    }

    public async Task<List<SubscriptionPackage>> GetActivePackagesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Where(sp => sp.IsActive)
            .OrderBy(sp => sp.DisplayOrder)
            .ToListAsync(cancellationToken);
    }
}
