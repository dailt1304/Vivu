using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Queries.GetUserBlogs
{
    public class GetUserBlogsQuery : PaginationRequest, IRequest<Result<PaginatedList<PublicBlogDto>>>
    {
        public Guid UserId { get; set; }
    }
}
