using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Queries.SearchPublicBlogs
{
    public class SearchPublicBlogsQuery : PaginationRequest, IRequest<Result<PaginatedList<PublicBlogDto>>>
    {
        public string SearchTerm { get; set; } = string.Empty;
    }
}
