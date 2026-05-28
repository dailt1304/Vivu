using MediatR;
using Vivu.Application.DTOs.Responses.BlogReports;
using Vivu.Domain.Enums;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Commands.FlagBlog;

public class FlagBlogCommand : IRequest<Result<ReportBlogResponse>>
{
    public Guid BlogId { get; set; }
    public BlogReportType ReportType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
}
