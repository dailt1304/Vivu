namespace Vivu.Application.DTOs.Responses.Payments;

public class TransactionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? PackageId { get; set; }
    public string? PackageName { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string Status { get; set; } = string.Empty;
    public long OrderCode { get; set; }
    public string? PayOSPaymentLinkId { get; set; }
    public string? CheckoutUrl { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
