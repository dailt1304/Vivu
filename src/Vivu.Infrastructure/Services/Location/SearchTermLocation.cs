using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Locations;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
namespace Vivu.Infrastructure.Services.Location
{
    public class SearchTermLocation : ISearchTermLocation
    {
        public List<LocationSearchResultDto> ApplyHighlightingInMemory(List<LocationSearchResultDto> items, string searchTerm)
        {
            var searchWords = searchTerm
                                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                .Where(w => w.Length >= 2)
                                .ToArray();

            foreach (var item in items)
            {
                item.NameHighlighted = HighlightText(item.Name, searchWords);
                item.DescriptionHighlighted = HighlightText(
                    item.Description,
                    searchWords,
                    maxLength: 200);
                item.AddressHighlighted = HighlightText(item.Address, searchWords);
            }
            return items;
        }

        public string ConvertToTsQuery(string searchTerm)
        {
            var words = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
                return string.Empty;

            var tsQueryParts = new List<string>();

            for (int i = 0; i < words.Length; i++)
            {
                if (i == words.Length - 1 && words[i].Length >= 2)
                {
                    tsQueryParts.Add($"{words[i]}:*");
                }
                else
                {
                    tsQueryParts.Add(words[i]);
                }
            }

            return string.Join(" & ", tsQueryParts);
        }

        public string? HighlightText(string? text, string[] searchWords, int? maxLength = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var result = text;

            foreach (var word in searchWords)
            {
                var pattern = $@"(?i)\b({Regex.Escape(word)})\b";
                result = Regex.Replace(
                    result,
                    pattern,
                    "<mark>$1</mark>",
                    RegexOptions.IgnoreCase);
            }

            if (maxLength.HasValue && text.Length > maxLength.Value)
            {
                var markIndex = result.IndexOf("<mark>", StringComparison.OrdinalIgnoreCase);

                if (markIndex >= 0)
                {
                    var start = Math.Max(0, markIndex - 50);
                    var end = Math.Min(result.Length, markIndex + maxLength.Value - 50);

                    result = (start > 0 ? "..." : "") +
                             result.Substring(start, end - start) +
                             (end < result.Length ? "..." : "");
                }
                else
                {
                    result = result.Substring(0, maxLength.Value) + "...";
                }
            }

            return result;
        }

        public string SanitizeSearchTerm(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return string.Empty;

            searchTerm = searchTerm.ToLower().Trim();

            searchTerm = searchTerm.Replace("đ", "d");

            searchTerm = RemoveDiacritics(searchTerm);

            searchTerm = Regex.Replace(searchTerm, @"[^\w\s\u00C0-\u1EF9]", " ");

            searchTerm = Regex.Replace(searchTerm, @"\s+", " ").Trim();

            searchTerm = searchTerm.ToLower();

            return searchTerm;
        }
        private string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }


    }
}
