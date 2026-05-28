using Vivu.Domain.Enums;
using Vivu.Domain.Shared;
using Vivu.Domain.Enums;

namespace Vivu.Domain.Entities;

public class LocationCategory : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public LocationCategoryType CategoryType { get; set; } = LocationCategoryType.Other;
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public virtual ICollection<Location> Locations { get; set; } = new List<Location>();
}
