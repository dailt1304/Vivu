using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivu.Domain.Entities;

namespace Vivu.Infrastructure.Data.Configurations;

public class TripMemberConfiguration : IEntityTypeConfiguration<TripMember>
{
    public void Configure(EntityTypeBuilder<TripMember> builder)
    {
        builder.ToTable("TripMembers");

        // Composite primary key
        builder.HasKey(tm => new { tm.TripId, tm.UserId });

        builder.Property(tm => tm.Role)
            .HasMaxLength(20)
            .HasDefaultValue("member");

        builder.Property(tm => tm.JoinedAt)
            .HasDefaultValueSql("now()");

        // Relationships
        builder.HasOne(tm => tm.Trip)
            .WithMany(t => t.TripMembers)
            .HasForeignKey(tm => tm.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tm => tm.User)
            .WithMany(u => u.TripMemberships)
            .HasForeignKey(tm => tm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tm => tm.Owner)
            .WithMany()
            .HasForeignKey(tm => tm.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
