namespace Vivu.Application.DTOs.Responses.LocationReports;

public class ReviewLocationReportResponse
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AdminNote { get; set; }
    public DateTime UpdatedAt { get; set; }
}
