using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.BlogReports.Commands.BanBlog;

public class BanBlogCommand : IRequest<Result<bool>>
{
    public Guid BlogId { get; set; }
}
