using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Commands.CreateBlogFromTrip
{
    public class CreateBlogFromTripCommand : IRequest<Result<BlogDto>>
    {
        public Guid TripId { get; set; }
        public string? Title { get; set; }
        public IFormFile? CoverImage { get; set; }

        public string? ShortDescription { get; set; }
        public decimal? TotalCost { get; set; }
        public List<string>? TagNames { get; set; }
        public List<UpdateBlogStoryDayDto>? StoryDays { get; set; }
    }
}
