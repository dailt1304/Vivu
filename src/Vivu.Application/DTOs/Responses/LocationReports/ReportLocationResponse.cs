namespace Vivu.Application.DTOs.Responses.LocationReports;

public class ReportLocationResponse
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}
