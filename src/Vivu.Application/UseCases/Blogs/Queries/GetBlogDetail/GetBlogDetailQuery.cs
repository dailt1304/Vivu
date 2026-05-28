using MediatR;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Queries.GetBlogDetail
{
    public class GetBlogDetailQuery : IRequest<Result<BlogDetailDto>>
    {
        /// Blog ID (Guid) or Slug (string). One of the two must be provided.
        public string IdOrSlug { get; set; } = string.Empty;
    }
}
