using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.BlogReports.Commands.UnbanBlog;

public class UnbanBlogCommand : IRequest<Result<bool>>
{
    public Guid BlogId { get; set; }
}
