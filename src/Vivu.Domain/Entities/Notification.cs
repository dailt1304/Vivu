using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class Notification : Entity<Guid>
{
    public Guid UserId { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Type { get; set; }
    public Guid? ReferenceId { get; set; }
    public bool IsRead { get; set; }

    // Navigation Properties
    public virtual User? User { get; set; }

    private Notification() { }

    public static Notification Create(
        Guid userId,
        string type,
        string? title = null,
        string? content = null,
        Guid? referenceId = null)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Content = content,
            ReferenceId = referenceId,
            IsRead = false,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
    }
}
