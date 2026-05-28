using Vivu.Domain.Shared;
using System.Globalization;
using System.Text;

namespace Vivu.Domain.Entities;

public class City : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? NameAscii { get; set; }
    public Guid CountryId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Image { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation Properties
    public virtual Country Country { get; set; } = null!;
    public virtual ICollection<Location> Locations { get; set; } = new List<Location>();
    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();

    public static City Create(
        string name,
        Guid countryId,
        decimal? latitude,
        decimal? longitude,
        string? image,
        Guid? creater,
        string? nameAscii = null)
    {
        return new City
        {
            Id = Guid.NewGuid(),
            Name = name,
            NameAscii = RemoveDiacritics(name),
            CountryId = countryId,
            Latitude = latitude,
            Longitude = longitude,
            Image = image,
            CreatedBy = creater.ToString(),
            CreatedDate = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Removes Vietnamese diacritics from a string.
    /// "Đà Nẵng" → "Da Nang", "Hồ Chí Minh" → "Ho Chi Minh"
    /// </summary>
    public static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        // Handle special Vietnamese character Đ/đ first (not decomposed by NormalizationForm.FormD)
        text = text.Replace('Đ', 'D').Replace('đ', 'd');

        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalizedString.Length);
        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
