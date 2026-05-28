using Vivu.Domain.Enums;
using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class User : Entity<Guid>
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsEmailVerified { get; set; }
    public string Status { get; set; } = "active";
    public DateTime? LastLoginAt { get; set; }
    public string? GoogleId { get; set; }
    public bool IsGoogleUser { get; set; } = false;

    public virtual UserProfile? UserProfile { get; set; }
    public virtual UserSettings? UserSettings { get; set; }
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
    public virtual ICollection<TripMember> TripMemberships { get; set; } = new List<TripMember>();
    public virtual ICollection<Blog> Blogs { get; set; } = new List<Blog>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual ICollection<Collection> Collections { get; set; } = new List<Collection>();
    private User()
    {
        UserRoles = new List<UserRole>();
    }

    public static User Create(string email, string passwordHash, string? phone = null,
                               string fullName = null, string avatarUrl = null, string bio = null, DateTime? dateOfBirth = null, string gender = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required", nameof(passwordHash));
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = email,
            PasswordHash = passwordHash,
            Phone = phone,
            Status = "active",
            IsEmailVerified = false,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
        };
        user.UserProfile = UserProfile.Create(user.Id, fullName, avatarUrl, bio, dateOfBirth, gender);
        return user;
    }

    public static User CreateGoogle(string email, string googleId, string fullName, string avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));

        if (string.IsNullOrWhiteSpace(googleId))
            throw new ArgumentException("GoogleId is required", nameof(googleId));
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = email,
            GoogleId = googleId,
            IsGoogleUser = true,
            IsEmailVerified = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
        };
        user.UserProfile = UserProfile.Create(user.Id, fullName, avatarUrl);
        return user;
    }

    public void VerifyEmail()
    {
        IsEmailVerified = true;
        ModifiedDate = DateTime.UtcNow;
    }

    public void UpdatePassword(string hash)
    {
        PasswordHash = hash;
        ModifiedDate = DateTime.UtcNow;
    }

    public void Ban()
    {
        Status = "banned";
        ModifiedDate = DateTime.UtcNow;
    }

    public void Unban()
    {
        Status = "active";
        ModifiedDate = DateTime.UtcNow;
    }

    public bool IsBanned()
    {
        return Status == "banned";
    }

    public void AssignRole(Guid roleId)
    {
        if (UserRoles.Any(ur => ur.RoleId == roleId))
            return; 

        UserRoles.Add(new UserRole
        {
            UserId = Id,
            RoleId = roleId
        });
    }

    public void RemoveRole(Guid roleId)
    {
        var userRole = UserRoles.FirstOrDefault(ur => ur.RoleId == roleId);
        if (userRole != null)
            UserRoles.Remove(userRole);
    }

    public bool HasRole(string roleName)
    {
        return UserRoles.Any(ur => ur.Role.RoleName == roleName.ToUpper());
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void UpdatePhone(string? phone)
    {
        if (!string.IsNullOrWhiteSpace(phone))
        {
            Phone = phone.Trim();
            ModifiedDate = DateTime.UtcNow;
        }
    }
}
