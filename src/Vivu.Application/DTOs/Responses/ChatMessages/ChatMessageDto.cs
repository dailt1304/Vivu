using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.DTOs.Responses.ChatMessages
{
    public class ChatMessageDto
    {
        public Guid Id { get; set; }
        public Guid TripId { get; set; }
        public Guid SenderId { get; set; }
        public string? SenderName { get; set; }
        public string? SenderAvatar { get; set; }
        public string? Content { get; set; }
        public string MessageType { get; set; } = "text";
        public bool IsAiMessage { get; set; }
        public Guid? ReplyToId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageFileName { get; set; }
    }
}
