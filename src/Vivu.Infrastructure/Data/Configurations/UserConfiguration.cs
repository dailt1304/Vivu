using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivu.Domain.Entities;

namespace Vivu.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);


        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.Phone)
            .HasMaxLength(20);

        builder.Property(u => u.IsEmailVerified)
            .HasDefaultValue(false);

        builder.Property(u => u.Status)
            .HasMaxLength(20)
            .HasDefaultValue("active");

        builder.Property(u => u.CreatedDate)
            .HasColumnName("CreatedAt")
            .HasDefaultValueSql("now()");

        builder.Property(u => u.ModifiedDate)
            .HasColumnName("UpdatedAt")
            .HasDefaultValueSql("now()");

        builder.Property(u => u.LastLoginAt)
            .HasColumnName("LastLoginAt");

        builder.Property(u => u.GoogleId)
            .HasMaxLength(255);

        builder.HasIndex(u => u.GoogleId)
            .IsUnique();

        builder.Property(u => u.IsGoogleUser)
            .HasDefaultValue(false);

        // Relationships
        builder.HasOne(u => u.UserProfile)
            .WithOne(up => up.User)
            .HasForeignKey<UserProfile>(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.UserSettings)
            .WithOne(us => us.User)
            .HasForeignKey<UserSettings>(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Trips)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Blogs)
            .WithOne(b => b.User)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Notifications)
            .WithOne(n => n.User)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
