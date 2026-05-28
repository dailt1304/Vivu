using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.DTOs.Responses.Locations
{
    public class PopularLocationDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public Guid? CityId { get; set; }
        public string? CityName { get; set; }

        public Guid? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryIconUrl { get; set; }

        public decimal RatingAverage { get; set; }
        public int RatingCount { get; set; }

        public string? ThumbnailUrl { get; set; }
    }
}
