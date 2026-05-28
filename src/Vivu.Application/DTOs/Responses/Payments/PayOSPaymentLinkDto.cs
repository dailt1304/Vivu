namespace Vivu.Application.DTOs.Responses.Payments;

public class CreatePaymentLinkResult
{
    public string PaymentLinkId { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public long OrderCode { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CancelPaymentResult
{
    public string PaymentLinkId { get; set; } = string.Empty;
    public long OrderCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Amount { get; set; }
}
