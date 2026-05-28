using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.BlogReports;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.BlogReports.Queries.GetBlogReports;

public class GetBlogReportsQuery : PaginationRequest, IRequest<Result<PaginatedList<BlogReportDto>>>
{
    public string? Status { get; set; }
    public string? ReportType { get; set; }
}
