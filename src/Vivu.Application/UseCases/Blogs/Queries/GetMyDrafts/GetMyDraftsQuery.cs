using MediatR;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Queries.GetMyDrafts
{
    public class GetMyDraftsQuery : IRequest<Result<List<PublicBlogDto>>>
    {
    }
}
