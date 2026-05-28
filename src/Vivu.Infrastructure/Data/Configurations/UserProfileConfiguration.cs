using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivu.Domain.Entities;

namespace Vivu.Infrastructure.Data.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");

        builder.HasKey(up => up.UserId);

        builder.Property(up => up.FullName)
            .HasMaxLength(100);

        builder.Property(up => up.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(up => up.Gender)
            .HasMaxLength(20);

        // One-to-One Relationship with User
        builder.HasOne(up => up.User)
            .WithOne(u => u.UserProfile)
            .HasForeignKey<UserProfile>(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship with Country
        builder.HasOne(up => up.Country)
            .WithMany(c => c.UserProfiles)
            .HasForeignKey(up => up.CountryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
