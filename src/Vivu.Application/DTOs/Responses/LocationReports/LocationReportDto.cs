namespace Vivu.Application.DTOs.Responses.LocationReports;

public class LocationReportDto
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string? LocationAddress { get; set; }
    public Guid? ReporterId { get; set; }
    public string? ReporterName { get; set; }
    public string? ReporterEmail { get; set; }
    public string? ReporterAvatar { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SuggestedName { get; set; }
    public string? SuggestedAddress { get; set; }
    public string? EvidenceImages { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AdminNote { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
