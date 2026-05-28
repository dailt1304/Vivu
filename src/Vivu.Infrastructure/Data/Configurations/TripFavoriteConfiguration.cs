using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivu.Domain.Entities;

namespace Vivu.Infrastructure.Data.Configurations;

public class TripFavoriteConfiguration : IEntityTypeConfiguration<TripFavorite>
{
    public void Configure(EntityTypeBuilder<TripFavorite> builder)
    {
        builder.ToTable("TripFavorites");

        // Composite primary key
        builder.HasKey(tf => new { tf.UserId, tf.TripId });

        builder.Property(tf => tf.CreatedAt)
            .HasDefaultValueSql("now()");

        // Relationships
        builder.HasOne(tf => tf.User)
            .WithMany()
            .HasForeignKey(tf => tf.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tf => tf.Trip)
            .WithMany(t => t.TripFavorites)
            .HasForeignKey(tf => tf.TripId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
