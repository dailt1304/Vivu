using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vivu.Domain.Entities;

namespace Vivu.Infrastructure.Data.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");

            builder.HasKey(rt => rt.Id);

            builder.Property(rt => rt.Token)
                .IsRequired()
                .HasMaxLength(512);

            builder.HasIndex(rt => rt.Token).IsUnique();

            builder.Property(rt => rt.IsUsed)
                .HasDefaultValue(false);
                
            builder.Property(rt => rt.IsRevoked)
                .HasDefaultValue(false);

            builder.Property(rt => rt.ExpiresAt)
                .HasColumnName("ExpiresAt")
                .IsRequired();

            builder.Property(rt => rt.RevokedAt)
                .HasColumnName("RevokedAt");

            builder.Property(rt => rt.RevokedReason)
                .HasColumnName("RevokedReason")
                .HasMaxLength(255);

            builder.Property(rt => rt.IpAddress)
                .HasColumnName("IpAddress")
                .HasMaxLength(45);

            builder.Property(rt => rt.DeviceType)
                .HasColumnName("DeviceType")
                .HasMaxLength(50);

            builder.Property(rt => rt.DeviceName)
                .HasColumnName("DeviceName")
                .HasMaxLength(100);

            builder.Property(rt => rt.CreatedDate)
                .HasColumnName("CreatedAt")
                .HasDefaultValueSql("now()");

            builder.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(rt => rt.ReplacedByToken)
                .WithOne()
                .HasForeignKey<RefreshToken>(rt => rt.ReplacedByTokenId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
