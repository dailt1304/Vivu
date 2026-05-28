using MediatR;
using Vivu.Application.DTOs.Responses.LocationReports;
using Vivu.Domain.Enums;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.LocationReports.Commands.ReviewLocationReport;

public class ReviewLocationReportCommand : IRequest<Result<ReviewLocationReportResponse>>
{
    public Guid ReportId { get; set; }
    public ReportStatus Status { get; set; }
    public string? AdminNote { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Images { get; set; }
}
