using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivu.Domain.Entities;

namespace Vivu.Infrastructure.Data.Configurations;

public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.ToTable("UserSettings");

        builder.HasKey(us => us.UserId);

        builder.Property(us => us.LanguagePreference)
            .HasMaxLength(10)
            .HasDefaultValue("en");

        builder.Property(us => us.UiTheme)
            .HasMaxLength(20)
            .HasDefaultValue("light");

        builder.Property(us => us.EnableEmail)
            .HasDefaultValue(true);

        builder.Property(us => us.EnablePush)
            .HasDefaultValue(true);
    }
}
