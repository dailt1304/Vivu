using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class Country : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }

    // Navigation Properties
    public virtual ICollection<City> Cities { get; set; } = new List<City>();
    public virtual ICollection<UserProfile> UserProfiles { get; set; } = new List<UserProfile>();
}
