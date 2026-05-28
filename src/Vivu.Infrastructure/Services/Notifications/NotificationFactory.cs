using Vivu.Application.Interfaces.Notifications;

namespace Vivu.Infrastructure.Services.Notifications
{
    public class NotificationFactory : INotificationFactory
    {
        public (string Title, string Content) CreateContent(string type)
        {
            return type switch
            {
                "TRIP_INVITE" => ("Chuyến đi mới", "Bạn đã được mời tham gia một chuyến đi."),
                "NEW_COMMENT" => ("Bình luận mới", "Có người vừa bình luận trong chuyến đi của bạn."),
                "PAYMENT_SUCCESS" => ("Thanh toán thành công", "Giao dịch thanh toán của bạn đã thành công."),
                _ => ("Thông báo", "Bạn có một thông báo mới.")
            };
        }
    }
}
