namespace Vivu.Application.DTOs.Responses.Payments;

public class PayOSPaymentInfoResult
{
    public string PaymentLinkId { get; set; } = string.Empty;
    public long OrderCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Amount { get; set; }
    public int AmountPaid { get; set; }
    public int AmountRemaining { get; set; }
    public List<PayOSTransaction> Transactions { get; set; } = new();
}

public class PayOSTransaction
{
    public string Reference { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TransactionDateTime { get; set; } = string.Empty;
    public string? CounterAccountBankId { get; set; }
    public string? CounterAccountBankName { get; set; }
    public string? CounterAccountName { get; set; }
    public string? CounterAccountNumber { get; set; }
    public string? VirtualAccountName { get; set; }
    public string? VirtualAccountNumber { get; set; }
}
