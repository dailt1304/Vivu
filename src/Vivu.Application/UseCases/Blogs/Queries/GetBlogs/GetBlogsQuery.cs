using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Queries.GetBlogs
{
    public class GetBlogsQuery : PaginationRequest, IRequest<Result<PaginatedList<PublicBlogDto>>>
    {
        public string SortBy { get; set; } = "newest";
        public string? Status { get; set; }
        public bool IsAdmin { get; set; }
    }
}
