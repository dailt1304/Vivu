using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NpgsqlTypes;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Domain.Entities;

namespace Vivu.Application.Interfaces.Locations
{
    public interface ISearchTermLocation
    {
        string SanitizeSearchTerm(string searchTerm);
        string ConvertToTsQuery(string searchTerm);
        List<LocationSearchResultDto> ApplyHighlightingInMemory(
                                List<LocationSearchResultDto> items,
                                string searchTerm);
        string? HighlightText(string? text, string[] searchWords, int? maxLength = null);
    }
}
