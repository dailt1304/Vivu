using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class FileAttachment : Entity<Guid>
{
    public Guid UploaderId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public long? FileSize { get; set; }
    public Guid? MessageId { get; set; }
    public string? FileName { get; set; }

    // Navigation Properties
    public virtual User Uploader { get; set; } = null!;
    public virtual ChatMessage? Message { get; set; }
}
