using System;

namespace Vivu.Application.DTOs.Responses.Collections
{
    public class CollectionLocationDto
    {
        public Guid LocationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? ImageUrl { get; set; }
        public decimal RatingAverage { get; set; }
        public string? CategoryName { get; set; }
        public string? CityName { get; set; }
        public string? Note { get; set; }
        public DateTime AddedAt { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
