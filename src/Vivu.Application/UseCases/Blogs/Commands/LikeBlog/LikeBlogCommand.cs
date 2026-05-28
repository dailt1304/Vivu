using MediatR;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Commands.LikeBlog
{
    public record LikeBlogCommand : IRequest<Result<LikeBlogResponse>>
    {
        public required Guid BlogId { get; init; }
    }
}
