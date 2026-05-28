using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class ChatMessage : Entity<Guid>
{
    public Guid TripId { get; set; }
    public Guid SenderId { get; set; }
    public string? Content { get; set; }
    public string MessageType { get; set; } = "text";
    public Guid? ReplyToId { get; set; }
    public bool IsAiMessage { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation Properties
    public virtual Trip Trip { get; set; } = null!;
    public virtual User Sender { get; set; } = null!;
    public virtual ChatMessage? ReplyTo { get; set; }
    public virtual ICollection<ChatMessage> Replies { get; set; } = new List<ChatMessage>();
    public virtual ICollection<FileAttachment> Files { get; set; } = new List<FileAttachment>();

    public static ChatMessage Create(
        Guid tripId,
        Guid senderId,
        string? content,
        string messageType = "text",
        bool isAiMessage = false,
        Guid? replyToId = null)
    {
        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            TripId = tripId,
            SenderId = senderId,
            Content = content,
            MessageType = messageType,
            IsAiMessage = isAiMessage,
            ReplyToId = replyToId,
            IsDeleted = false,
            CreatedBy = senderId.ToString(),
            CreatedDate = DateTime.UtcNow
        };
    }
}
