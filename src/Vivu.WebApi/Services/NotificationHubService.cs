using Microsoft.AspNetCore.SignalR;
using Vivu.Application.DTOs.Responses.Notifications;
using Vivu.Application.Interfaces.Notifications;
using Vivu.WebApi.Hubs;

namespace Vivu.WebApi.Services
{
    public class NotificationHubService : INotificationHubService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationHubService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendNotificationAsync(Guid userId, NotificationDto notification)
        {
            await _hubContext.Clients.Group($"user-{userId}")
                .SendAsync("ReceiveNotification", notification);
        }

        public async Task SendUnreadCountAsync(Guid userId, int unreadCount)
        {
            await _hubContext.Clients.Group($"user-{userId}")
                .SendAsync("UnreadCountUpdated", unreadCount);
        }
    }
}
