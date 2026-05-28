using MediatR;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Commands.CreateComment
{
    public record CreateCommentCommand : IRequest<Result<BlogCommentDto>>
    {
        public required Guid BlogId { get; init; }
        public required string Content { get; init; }
    }
}
