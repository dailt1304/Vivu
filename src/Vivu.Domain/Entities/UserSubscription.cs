using Vivu.Domain.Enums;
using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class UserSubscription : Entity<Guid>
{
    public Guid UserId { get; set; }
    public Guid PackageId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;

    public static UserSubscription Create(Guid userId, SubscriptionPackage package, DateTime? now = null)
    {
        var current = now ?? DateTime.UtcNow;

        return new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PackageId = package.Id,
            StartDate = current,
            EndDate = current.AddDays(package.DurationDays),
            Status = SubscriptionStatus.Active.ToString(),
            CreatedDate = current,
            Package = package
        };
    }
    public void Expire()
    {
        Status = SubscriptionStatus.Expired.ToString();
        ModifiedDate = DateTime.UtcNow;
    }
    public bool IsExpired(DateTime? now = null)
    {
        var current = now ?? DateTime.UtcNow;
        return EndDate < current;
    }
    public int DaysRemaining(DateTime? now = null)
    {
        var current = now ?? DateTime.UtcNow;
        var remaining = (EndDate - current).Days;
        return Math.Max(0, remaining);
    }

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual SubscriptionPackage Package { get; set; } = null!;
    public virtual ICollection<ApiUsageLog> ApiUsageLogs { get; set; } = new List<ApiUsageLog>();
}
