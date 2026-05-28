using System;

namespace Vivu.Application.DTOs.Responses.Collections
{
    public class CollectionSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int LocationCount { get; set; }
    }
}
