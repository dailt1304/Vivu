namespace Vivu.Domain.Entities;

public class UserRole
{
    public Guid RoleId { get; set; }
    public Guid UserId { get; set; }

    // Navigation Properties
    public virtual Role Role { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
