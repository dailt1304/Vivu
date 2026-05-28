using System;
using System.Collections.Generic;

namespace Vivu.Application.DTOs.Responses.Collections
{
    public class CollectionDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CoverImageUrl { get; set; }
        public int LocationCount { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<CollectionLocationDto> Locations { get; set; } = new();
    }
}
