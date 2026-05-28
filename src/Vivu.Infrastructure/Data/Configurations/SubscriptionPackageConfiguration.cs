using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivu.Domain.Entities;

namespace Vivu.Infrastructure.Data.Configurations;

public class SubscriptionPackageConfiguration : IEntityTypeConfiguration<SubscriptionPackage>
{
    public void Configure(EntityTypeBuilder<SubscriptionPackage> builder)
    {
        builder.Property(sp => sp.Feature)
            .HasColumnType("jsonb");
    }
}
