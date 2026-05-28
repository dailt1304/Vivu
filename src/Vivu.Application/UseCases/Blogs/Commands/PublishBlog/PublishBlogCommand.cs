using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Commands.PublishBlog
{
    public record PublishBlogCommand : IRequest<Result<BlogDto>>
    {
        public required Guid BlogId { get; init; }
    }
}
