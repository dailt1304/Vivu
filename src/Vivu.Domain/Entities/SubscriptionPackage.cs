using Vivu.Domain.Enums;
using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class SubscriptionPackage : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Feature { get; set; }
    public string? Code { get; set; }
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public int MaxAiRequestPerDay { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsRecommended { get; set; }

    // Navigation Properties
    public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();

    public static SubscriptionPackage Create(
        string name,
        decimal price,
        int durationDays,
        int maxAiRequestPerDay,
        SubscriptionType? type = null,
        string? feature = null,
        string? code = null,
        string? description = null,
        bool isActive = true,
        int displayOrder = 0,
        bool isRecommended = false)
    {

        return new SubscriptionPackage
        {
            Id = Guid.NewGuid(),
            Name = name,
            Feature = feature,
            Code = code,
            Description = description,
            Price = price,
            DurationDays = durationDays,
            MaxAiRequestPerDay = maxAiRequestPerDay,
            IsActive = isActive,
            Type = type.ToString(),
            DisplayOrder = displayOrder,
            IsRecommended = isRecommended,
            CreatedDate = DateTime.UtcNow
        };
    }

    public void Update(
        string name,
        decimal price,
        int durationDays,
        int maxAiRequestPerDay,
        SubscriptionType? type = null,
        string? feature = null,
        string? code = null,
        string? description = null,
        bool isActive = true,
        int displayOrder = 0,
        bool isRecommended = false)
    {
        Name = name;
        Feature = feature;
        Code = code;
        Description = description;
        Price = price;
        DurationDays = durationDays;
        MaxAiRequestPerDay = maxAiRequestPerDay;
        IsActive = isActive;
        Type = type.ToString();
        DisplayOrder = displayOrder;
        IsRecommended = isRecommended;
        ModifiedDate = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        ModifiedDate = DateTime.UtcNow;
    }
}
