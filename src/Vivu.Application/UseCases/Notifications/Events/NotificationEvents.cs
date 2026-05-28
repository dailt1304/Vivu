using System;
using MediatR;

namespace Vivu.Application.UseCases.Notifications.Events
{
    /// <summary>
    /// Base class for all notification domain events.
    /// Published via MediatR INotification for decoupling.
    /// </summary>
    public abstract class NotificationEvent : INotification
    {
        public Guid UserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Content { get; set; }
        public Guid? ReferenceId { get; set; }
    }

    // ===== Trip Events =====

    public class MemberJoinedTripEvent : NotificationEvent
    {
        public MemberJoinedTripEvent(Guid tripOwnerId, Guid tripId, string memberName, string tripTitle)
        {
            UserId = tripOwnerId;
            Type = "MEMBER_JOINED";
            Title = "Thành viên mới";
            Content = $"{memberName} đã tham gia chuyến đi \"{tripTitle}\".";
            ReferenceId = tripId;
        }
    }

    public class MemberLeftTripEvent : NotificationEvent
    {
        public MemberLeftTripEvent(Guid tripOwnerId, Guid tripId, string memberName, string tripTitle)
        {
            UserId = tripOwnerId;
            Type = "MEMBER_LEFT";
            Title = "Thành viên rời đi";
            Content = $"{memberName} đã rời khỏi chuyến đi \"{tripTitle}\".";
            ReferenceId = tripId;
        }
    }

    public class MemberRemovedFromTripEvent : NotificationEvent
    {
        public MemberRemovedFromTripEvent(Guid removedUserId, Guid tripId, string tripTitle)
        {
            UserId = removedUserId;
            Type = "MEMBER_REMOVED";
            Title = "Bị xóa khỏi chuyến đi";
            Content = $"Bạn đã bị xóa khỏi chuyến đi \"{tripTitle}\".";
            ReferenceId = tripId;
        }
    }

    // ===== Blog Events =====

    public class NewCommentEvent : NotificationEvent
    {
        public NewCommentEvent(Guid blogOwnerId, Guid blogId, string commenterName, string blogTitle)
        {
            UserId = blogOwnerId;
            Type = "NEW_COMMENT";
            Title = "Bình luận mới";
            Content = $"{commenterName} đã bình luận trong bài viết \"{blogTitle}\".";
            ReferenceId = blogId;
        }
    }

    public class NewLikeEvent : NotificationEvent
    {
        public NewLikeEvent(Guid blogOwnerId, Guid blogId, string likerName, string blogTitle)
        {
            UserId = blogOwnerId;
            Type = "NEW_LIKE";
            Title = "Lượt thích mới";
            Content = $"{likerName} đã thích bài viết \"{blogTitle}\".";
            ReferenceId = blogId;
        }
    }

    // ===== Payment Events =====

    public class PaymentSuccessEvent : NotificationEvent
    {
        public PaymentSuccessEvent(Guid userId, Guid transactionId, string? packageName)
        {
            UserId = userId;
            Type = "PAYMENT_SUCCESS";
            Title = "Thanh toán thành công";
            Content = $"Thanh toán gói \"{packageName ?? "Premium"}\" đã thành công.";
            ReferenceId = transactionId;
        }
    }

    public class PaymentFailedEvent : NotificationEvent
    {
        public PaymentFailedEvent(Guid userId, Guid transactionId)
        {
            UserId = userId;
            Type = "PAYMENT_FAILED";
            Title = "Thanh toán thất bại";
            Content = "Giao dịch thanh toán của bạn đã bị hủy hoặc thất bại.";
            ReferenceId = transactionId;
        }
    }

    // ===== AI Events =====

    public class AIQuotaLowEvent : NotificationEvent
    {
        public AIQuotaLowEvent(Guid userId, int remaining, int limit)
        {
            UserId = userId;
            Type = "AI_QUOTA_LOW";
            Title = "Sắp hết lượt AI";
            Content = $"Bạn còn {remaining}/{limit} lượt AI hôm nay. Nâng cấp Premium để có thêm!";
        }
    }

    public class AIQuotaExceededEvent : NotificationEvent
    {
        public AIQuotaExceededEvent(Guid userId, int limit)
        {
            UserId = userId;
            Type = "AI_QUOTA_EXCEEDED";
            Title = "Hết lượt AI";
            Content = $"Bạn đã sử dụng hết {limit} lượt AI hôm nay. Nâng cấp Premium hoặc quay lại ngày mai!";
        }
    }

    // ===== Subscription Lifecycle Events =====

    public class SubscriptionExpiredEvent : NotificationEvent
    {
        public SubscriptionExpiredEvent(Guid userId)
        {
            UserId = userId;
            Type = "SUBSCRIPTION_EXPIRED";
            Title = "Gói đăng ký đã hết hạn";
            Content = "Gói Premium của bạn đã hết hạn. Mua gói mới để tiếp tục sử dụng!";
        }
    }

    public class SubscriptionNearExpiryEvent : NotificationEvent
    {
        public SubscriptionNearExpiryEvent(Guid userId, int daysRemaining)
        {
            UserId = userId;
            Type = "SUBSCRIPTION_NEAR_EXPIRY";
            Title = "Gói sắp hết hạn";
            Content = $"Gói Premium của bạn sẽ hết hạn trong {daysRemaining} ngày.";
        }
    }
}
