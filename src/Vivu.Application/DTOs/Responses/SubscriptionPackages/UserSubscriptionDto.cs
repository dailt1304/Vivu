namespace Vivu.Application.DTOs.Responses.SubscriptionPackages;

public class UserSubscriptionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public string? PackageCode { get; set; }
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public int MaxAiRequestPerDay { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
}