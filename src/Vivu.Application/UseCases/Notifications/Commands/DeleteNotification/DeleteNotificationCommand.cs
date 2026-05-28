using System;
using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Notifications.Commands.DeleteNotification
{
    public class DeleteNotificationCommand : IRequest<Result<bool>>
    {
        public Guid NotificationId { get; set; }
    }
}
