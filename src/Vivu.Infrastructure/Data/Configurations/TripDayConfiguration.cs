using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivu.Domain.Entities;

namespace Vivu.Infrastructure.Data.Configurations;

public class TripDayConfiguration : IEntityTypeConfiguration<TripDay>
{
    public void Configure(EntityTypeBuilder<TripDay> builder)
    {
        builder.ToTable("TripDays");

        builder.HasKey(td => td.Id);

        builder.HasOne(td => td.Trip)
            .WithMany(t => t.TripDays)
            .HasForeignKey(td => td.TripId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}