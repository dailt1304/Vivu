using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;

namespace Vivu.Infrastructure.Data.Configurations;

public class LocationCategoryConfiguration : IEntityTypeConfiguration<LocationCategory>
{
    public void Configure(EntityTypeBuilder<LocationCategory> builder)
    {
        builder.ToTable("LocationCategories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.IconUrl)
            .HasMaxLength(500);

        builder.Property(c => c.CategoryType)
            .HasConversion<int>()
            .HasColumnType("integer")
            .HasDefaultValue(LocationCategoryType.Other);
    }
}
