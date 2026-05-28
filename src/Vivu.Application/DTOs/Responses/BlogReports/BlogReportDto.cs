namespace Vivu.Application.DTOs.Responses.BlogReports;

public class BlogReportDto
{
    public Guid Id { get; set; }
    public Guid BlogId { get; set; }
    public string BlogTitle { get; set; } = string.Empty;
    public Guid? ReporterId { get; set; }
    public string? ReporterName { get; set; }
    public string? ReporterEmail { get; set; }
    public string? ReporterAvatar { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AdminNote { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
