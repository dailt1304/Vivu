using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Vivu.Application.Interfaces.Trips;

namespace Vivu.Infrastructure.Services.Trips
{
    public class SearchTermTrip : ISearchTermTrip
    {
        public string SanitizeSearchTerm(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return string.Empty;

            searchTerm = searchTerm.ToLower().Trim();

            // Handle Vietnamese 'đ' specifically
            searchTerm = searchTerm.Replace("đ", "d");

            // Remove diacritics
            searchTerm = RemoveDiacritics(searchTerm);

            // Remove special characters, keep only letters, numbers, and spaces
            searchTerm = Regex.Replace(searchTerm, @"[^\w\s\u00C0-\u1EF9]", " ");

            // Normalize whitespace
            searchTerm = Regex.Replace(searchTerm, @"\s+", " ").Trim();

            return searchTerm.ToLower();
        }

        public string ConvertToTsQuery(string sanitizedTerm)
        {
            if (string.IsNullOrWhiteSpace(sanitizedTerm))
                return string.Empty;

            var words = sanitizedTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
                return string.Empty;

            var tsQueryParts = new List<string>();

            // Add prefix matching for the last word
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

