using Microsoft.EntityFrameworkCore;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;

namespace Vivu.Infrastructure.Repository;

public class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
{
    public TransactionRepository(VivuDbContext context) : base(context) { }

    public async Task<Transaction?> GetByOrderCodeAsync(long orderCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.OrderCode == orderCode, cancellationToken);
    }

    public async Task<Transaction?> GetByPaymentLinkIdAsync(string paymentLinkId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.PayOSPaymentLinkId == paymentLinkId, cancellationToken);
    }

    public async Task<List<Transaction>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Include(t => t.Package)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Transaction?> GetPendingByUserAndPackageAsync(Guid userId, Guid packageId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(t =>
                t.UserId == userId &&
                t.PackageId == packageId &&
                t.Status == PaymentStatus.Pending.ToString(),
                cancellationToken);
    }

    public async Task<Transaction?> GetByIdWithDetailsAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(t => t.User)
            .ThenInclude(u => u.UserProfile)
            .Include(t => t.Package)
            .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);
    }

    // Statistics
    public async Task<decimal> GetRevenueInPeriodAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(t => t.Status == PaymentStatus.Paid.ToString() && t.CreatedDate >= from && t.CreatedDate <= to)
            .SumAsync(t => t.Amount, ct);
    }

    public async Task<int> GetTransactionCountByStatusAsync(string status, DateTime from, DateTime to, CancellationToken ct = default)
        => await _dbSet.CountAsync(t =>
            t.Status == status && t.CreatedDate >= from && t.CreatedDate <= to, ct);
}
