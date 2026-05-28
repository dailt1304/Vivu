using System;

namespace Vivu.Application.DTOs.Responses.Notifications
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? Type { get; set; }
        public Guid? ReferenceId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
