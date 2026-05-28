using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class Role : Entity<Guid>
{
    public string RoleName { get; set; } = string.Empty;
    public string? RoleDescription { get; set; }

    // Navigation Properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public static Role Create(string roleName, string roleDescription = null)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            throw new ArgumentException("Role name is required", nameof(roleName));

        return new Role
        {
            Id = Guid.NewGuid(),
            RoleName = roleName.ToUpper(),
            RoleDescription = roleDescription
        };
    }

    public static class Names
    {
        public const string Admin = "ADMIN";
        public const string Moderator = "MODERATOR";
        public const string User = "USER";
        public const string Premium = "PREMIUM";
    }
}
