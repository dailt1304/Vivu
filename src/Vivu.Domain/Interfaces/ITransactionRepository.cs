using Vivu.Domain.Entities;

namespace Vivu.Domain.Interfaces;

public interface ITransactionRepository : IGenericRepository<Transaction>
{
    Task<Transaction?> GetByOrderCodeAsync(long orderCode, CancellationToken cancellationToken = default);
    Task<Transaction?> GetByPaymentLinkIdAsync(string paymentLinkId, CancellationToken cancellationToken = default);
    Task<List<Transaction>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Transaction?> GetPendingByUserAndPackageAsync(Guid userId, Guid packageId, CancellationToken cancellationToken = default);
    Task<Transaction?> GetByIdWithDetailsAsync(Guid transactionId, CancellationToken cancellationToken = default);

    // Statistics
    Task<decimal> GetRevenueInPeriodAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<int> GetTransactionCountByStatusAsync(string status, DateTime from, DateTime to, CancellationToken ct = default);
}
