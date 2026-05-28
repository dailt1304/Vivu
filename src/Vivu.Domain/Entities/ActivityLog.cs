using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class ActivityLog : Entity<Guid>
{
    public string Action { get; set; } = string.Empty;
    public string? Metadata { get; set; }
}
