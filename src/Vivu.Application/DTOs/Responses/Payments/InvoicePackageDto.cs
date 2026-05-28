namespace Vivu.Application.DTOs.Responses.Payments;

public class InvoicePackageDto
{
    public Guid PackageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public int DurationDays { get; set; }
    public int MaxAiRequestPerDay { get; set; }
    public decimal Price { get; set; }
}
