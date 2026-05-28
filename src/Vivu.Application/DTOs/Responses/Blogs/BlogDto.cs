using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.DTOs.Responses.Blogs
{
    public class BlogDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? TripId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public string? ShortDescription { get; set; }
        public DateTime? TravelDateStart { get; set; }
        public DateTime? TravelDateEnd { get; set; }
        public decimal? TotalCost { get; set; }
        public int? GroupSize { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<BlogStoryDayDto> BlogStoryDays { get; set; } = new();
    }
}
