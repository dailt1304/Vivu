namespace Vivu.Application.DTOs.Responses.Payments;

public class InvoiceCustomerDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Phone { get; set; }
}
