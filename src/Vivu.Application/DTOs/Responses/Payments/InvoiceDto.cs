namespace Vivu.Application.DTOs.Responses.Payments;

public class InvoiceDto
{
    public Guid TransactionId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    
    public InvoiceCustomerDto Customer { get; set; } = null!;
    public InvoicePackageDto Package { get; set; } = null!;
    public InvoicePaymentDto Payment { get; set; } = null!;
    
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
