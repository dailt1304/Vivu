using System;
using MediatR;
using Vivu.Application.DTOs.Responses.Notifications;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Notifications.Commands.CreateNotification
{
    public class CreateNotificationCommand : IRequest<Result<NotificationDto>>
    {
        public Guid UserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Content { get; set; }
        public Guid? ReferenceId { get; set; }
    }
}
