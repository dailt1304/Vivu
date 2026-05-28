using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Commands.UpdateBlog
{
    public record UpdateBlogCommand : IRequest<Result<BlogDto>>
    {
        public required Guid BlogId { get; set; }
        public string? Title { get; init; }
        public string? ShortDescription { get; init; }
        public decimal? TotalCost { get; init; }
        public int? GroupSize { get; init; }
        public IFormFile? CoverImage { get; set; }
        public List<string>? TagNames { get; init; }
        public List<UpdateBlogStoryDayDto>? StoryDays { get; init; }
    }
}
