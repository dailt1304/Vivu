namespace Vivu.Domain.Entities;

public class UserProfile
{
    public Guid UserId { get; set; }
    
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public Guid? CountryId { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual Country? Country { get; set; }

    public static UserProfile Create(Guid userId, string fullName = null, string avatarUrl = null, string bio = null, DateTime? dateOfBirth = null,
                                        string genDer = null)
    {
        return new UserProfile
        {
            UserId = userId,
            FullName = fullName ?? "user",
            AvatarUrl = avatarUrl,
            Bio = bio,
            DateOfBirth = dateOfBirth,
            Gender = genDer
        };
    }

    public void Update(
        string? fullName = null,
        string? bio = null,
        DateTime? dateOfBirth = null,
        string? gender = null,
        Guid? countryId = null,
        string? avatarUrl = null)
    {
        if (fullName != null)
            FullName = fullName.Trim();

        if (bio != null)
            Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();

        if (dateOfBirth.HasValue)
            DateOfBirth = dateOfBirth.Value.Date;

        if (gender != null)
            Gender = gender.Trim();

        if (countryId.HasValue)
            CountryId = countryId.Value;

        if (avatarUrl != null)
            AvatarUrl = avatarUrl.Trim();
    }
}
