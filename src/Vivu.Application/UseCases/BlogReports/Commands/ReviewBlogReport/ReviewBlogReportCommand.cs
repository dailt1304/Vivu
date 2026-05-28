using MediatR;
using Vivu.Application.DTOs.Responses.BlogReports;
using Vivu.Domain.Enums;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.BlogReports.Commands.ReviewBlogReport;

public class ReviewBlogReportCommand : IRequest<Result<ReviewBlogReportResponse>>
{
    public Guid ReportId { get; set; }
    public ReportStatus Status { get; set; }
    public string? AdminNote { get; set; }
}
