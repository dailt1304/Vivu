using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Commands.DeleteBlog
{
    public record DeleteBlogCommand : IRequest<Result<bool>>
    {
        public required Guid BlogId { get; init; }
    }
}
