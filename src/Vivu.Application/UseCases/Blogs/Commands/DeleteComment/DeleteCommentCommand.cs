using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Commands.DeleteComment
{
    public record DeleteCommentCommand : IRequest<Result<bool>>
    {
        public required Guid CommentId { get; init; }
    }
}
