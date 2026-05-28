using System;
using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Notifications;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Notifications.Queries.GetUserNotifications
{
    public class GetUserNotificationsQuery : PaginationRequest, IRequest<Result<PaginatedList<NotificationDto>>>
    {
        public Guid UserId { get; set; }
        public bool? UnreadOnly { get; set; }
        public string? Type { get; set; }
    }
}
