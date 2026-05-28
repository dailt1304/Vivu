using Vivu.Domain.Enums;

namespace Vivu.Application.DTOs.Responses.SubscriptionPackages;

public class SubscriptionPackageDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Feature { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public int MaxAiRequestPerDay { get; set; }
    public bool IsActive { get; set; }
    public string? Type { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsRecommended { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
