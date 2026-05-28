using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Vivu.Application.Interfaces.Locations;

namespace Vivu.Infrastructure.Services.Location
{
    public class HelperLocation : IHelperLocation
    {
        public string? ConvertImagesToJson(string? images)
        {
            if (string.IsNullOrWhiteSpace(images))
                return null;

            // If already valid JSON array, return as-is
            var trimmed = images.Trim();
            if (trimmed.StartsWith("["))
                return trimmed;

            // Convert comma-separated URLs to JSON array
            var urls = trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                              .Where(u => !string.IsNullOrWhiteSpace(u))
                              .ToList();

            return urls.Count > 0 ? JsonSerializer.Serialize(urls) : null;
        }

        public string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
