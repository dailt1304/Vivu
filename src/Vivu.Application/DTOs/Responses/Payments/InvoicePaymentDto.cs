namespace Vivu.Application.DTOs.Responses.Payments;

public class InvoicePaymentDto
{
    public long OrderCode { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PayOSPaymentLinkId { get; set; }
    public DateTime TransactionDate { get; set; }
}
