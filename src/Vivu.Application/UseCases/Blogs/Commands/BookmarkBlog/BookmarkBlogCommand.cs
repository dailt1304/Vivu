using MediatR;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Commands.BookmarkBlog
{
    public record BookmarkBlogCommand : IRequest<Result<BookmarkBlogResponse>>
    {
        public required Guid BlogId { get; init; }
    }
}
