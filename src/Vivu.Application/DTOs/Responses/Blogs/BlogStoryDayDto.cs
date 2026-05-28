using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.DTOs.Responses.Blogs
{
    public class BlogStoryDayDto
    {
        public Guid Id { get; set; }
        public string BlockType { get; set; } = "location";
        public int DayNumber { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? DestinationName { get; set; }
        public Guid? LocationId { get; set; }
        public string? ImageUrl { get; set; }
        public string? QuoteAuthor { get; set; }
        public int DisplayOrder { get; set; }
    }
}
