using Vivu.Application.DTOs.Responses.Notifications;

namespace Vivu.Application.Interfaces.Notifications
{
    public interface INotificationHubService
    {
        Task SendNotificationAsync(Guid userId, NotificationDto notification);
        Task SendUnreadCountAsync(Guid userId, int unreadCount);
    }
}
