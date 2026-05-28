using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Queries.GetMyBookmarks
{
    public class GetMyBookmarksQuery : PaginationRequest, IRequest<Result<PaginatedList<PublicBlogDto>>>
    {
    }
}
