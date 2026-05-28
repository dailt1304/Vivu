using System;
using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Notifications.Commands.MarkNotificationAsRead
{
    public class MarkNotificationAsReadCommand : IRequest<Result<bool>>
    {
        public Guid NotificationId { get; set; }
    }
}
