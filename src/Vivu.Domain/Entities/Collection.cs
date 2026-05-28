using System;
using System.Collections.Generic;
using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities
{
public class Collection : Entity<Guid>
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<CollectionLocation> CollectionLocations { get; set; } = new List<CollectionLocation>();

        private Collection() { } // EF Core

        // Factory method
        public static Collection Create(Guid userId, string name, string? description = null, string? coverImageUrl = null)
        {
            return new Collection
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = name,
                Description = description,
                CoverImageUrl = coverImageUrl,
                CreatedDate = DateTime.UtcNow
            };
        }

        public void Update(string name, string? description = null)
        {
            Name = name;
            Description = description;
            ModifiedDate = DateTime.UtcNow;
        }

        public void UpdateCoverImage(string? coverImageUrl)
        {
            CoverImageUrl = coverImageUrl;
            ModifiedDate = DateTime.UtcNow;
        }

        public void Delete()
        {
            IsDeleted = true;
            ModifiedDate = DateTime.UtcNow;
        }
    }
}
