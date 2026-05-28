namespace Vivu.Application.DTOs.Responses.Payments;

public class CreatePaymentResponse
{
    public Guid TransactionId { get; set; }
    public string CheckoutUrl { get; set; } = string.Empty;
    public long OrderCode { get; set; }
    public string Status { get; set; } = string.Empty;
}
