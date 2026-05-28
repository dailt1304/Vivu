using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Queries.GetComments
{
    public class GetCommentsQuery : PaginationRequest, IRequest<Result<PaginatedList<BlogCommentDto>>>
    {
        public required Guid BlogId { get; init; }
    }
}
