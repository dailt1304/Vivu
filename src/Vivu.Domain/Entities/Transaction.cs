using Vivu.Domain.Enums;
using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class Transaction : Entity<Guid>
{
    public Guid UserId { get; set; }
    public Guid? PackageId { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string Status { get; set; } = PaymentStatus.Pending.ToString();

    // PayOS fields
    public long OrderCode { get; set; }
    public string? PayOSPaymentLinkId { get; set; }
    public string? CheckoutUrl { get; set; }
    public string? Description { get; set; }

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual SubscriptionPackage? Package { get; set; }

    public static Transaction Create(
        Guid userId,
        Guid packageId,
        decimal amount,
        string paymentMethod,
        long orderCode,
        string paymentLinkId,
        string checkoutUrl,
        string? description = null)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PackageId = packageId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            Status = PaymentStatus.Pending.ToString(),
            OrderCode = orderCode,
            PayOSPaymentLinkId = paymentLinkId,
            CheckoutUrl = checkoutUrl,
            Description = description,
            CreatedDate = DateTime.UtcNow
        };
    }
}
