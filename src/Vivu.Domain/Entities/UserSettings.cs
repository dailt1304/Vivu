using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class UserSettings : Entity<Guid>
{
    public Guid UserId { get; set; }
    public string LanguagePreference { get; set; } = "en";
    public string UiTheme { get; set; } = "light";
    public bool EnableEmail { get; set; } = true;
    public bool EnablePush { get; set; } = true;

    // Navigation Properties
    public virtual User User { get; set; } = null!;
}
