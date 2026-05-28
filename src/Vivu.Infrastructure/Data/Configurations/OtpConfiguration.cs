using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivu.Domain.Entities;

namespace Vivu.Infrastructure.Data.Configurations;

public class OtpConfiguration : IEntityTypeConfiguration<Otp>
{
    public void Configure(EntityTypeBuilder<Otp> builder)
    {
        builder.ToTable("Otp");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(o => o.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(o => o.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(o => o.IsUsed)
            .HasDefaultValue(false);

        builder.Property(o => o.IsVerified)
            .HasDefaultValue(false);

        builder.Property(o => o.VerifyAttempts)
            .HasDefaultValue(0);

        builder.Property(o => o.ExpiredAt)
            .IsRequired();

        builder.Property(o => o.CreatedDate)
            .HasColumnName("CreatedAt")
            .HasDefaultValueSql("now()");

        // Relationship
        builder.HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
