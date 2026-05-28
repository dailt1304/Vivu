using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Notifications;
using Vivu.Application.Interfaces.Cache;
using Vivu.Application.Interfaces.Notifications;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;

namespace Vivu.Application.UseCases.Notifications.Events
{
    /// <summary>
    /// Unified handler that listens to ALL NotificationEvent subclasses
    /// and creates a Notification entity in the database.
    /// </summary>
    public class NotificationEventHandler :
        INotificationHandler<MemberJoinedTripEvent>,
        INotificationHandler<MemberLeftTripEvent>,
        INotificationHandler<MemberRemovedFromTripEvent>,
        INotificationHandler<NewCommentEvent>,
        INotificationHandler<NewLikeEvent>,
        INotificationHandler<PaymentSuccessEvent>,
        INotificationHandler<PaymentFailedEvent>,
        INotificationHandler<AIQuotaLowEvent>,
        INotificationHandler<AIQuotaExceededEvent>,
        INotificationHandler<SubscriptionExpiredEvent>,
        INotificationHandler<SubscriptionNearExpiryEvent>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly INotificationFactory _notificationFactory;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly INotificationHubService _hubService;
        private readonly IMapper _mapper;
        private readonly ILogger<NotificationEventHandler> _logger;

        public NotificationEventHandler(
            INotificationRepository notificationRepository,
            INotificationFactory notificationFactory,
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            INotificationHubService hubService,
            IMapper mapper,
            ILogger<NotificationEventHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _notificationFactory = notificationFactory;
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _hubService = hubService;
            _mapper = mapper;
            _logger = logger;
        }

        public Task Handle(MemberJoinedTripEvent notification, CancellationToken cancellationToken)
            => CreateNotificationAsync(notification, cancellationToken);

        public Task Handle(MemberLeftTripEvent notification, CancellationToken cancellationToken)
            => CreateNotificationAsync(notification, cancellationToken);

        public Task Handle(MemberRemovedFromTripEvent notification, CancellationToken cancellationToken)
            => CreateNotificationAsync(notification, cancellationToken);

        public Task Handle(NewCommentEvent notification, CancellationToken cancellationToken)
            => CreateNotificationAsync(notification, cancellationToken);

        public Task Handle(NewLikeEvent notification, CancellationToken cancellationToken)
            => CreateNotificationAsync(notification, cancellationToken);

        public Task Handle(PaymentSuccessEvent notification, CancellationToken cancellationToken)
            => CreateNotificationAsync(notification, cancellationToken);

        public Task Handle(PaymentFailedEvent notification, CancellationToken cancellationToken)
            => CreateNotificationAsync(notification, cancellationToken);

        public Task Handle(AIQuotaLowEvent notification, CancellationToken cancellationToken)
            => CreateNotificationAsync(notification, cancellationToken);

        public Task Handle(AIQuotaExceededEvent notification, CancellationToken cancellationToken)
            => CreateNotificationAsync(notification, cancellationToken);

        public Task Handle(SubscriptionExpiredEvent notification, CancellationToken cancellationToken)
            => CreateNotificationAsync(notification, cancellationToken);

        public Task Handle(SubscriptionNearExpiryEvent notification, CancellationToken cancellationToken)
            => CreateNotificationAsync(notification, cancellationToken);

        private async Task CreateNotificationAsync(NotificationEvent evt, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Creating notification for event {Type} -> User {UserId}", evt.Type, evt.UserId);

                string title = evt.Title ?? string.Empty;
                string content = evt.Content ?? string.Empty;

                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
                {
                    var generated = _notificationFactory.CreateContent(evt.Type);
                    title = string.IsNullOrWhiteSpace(title) ? generated.Title : title;
                    content = string.IsNullOrWhiteSpace(content) ? generated.Content : content;
                }

                var notification = Notification.Create(
                    userId: evt.UserId,
                    type: evt.Type,
                    title: title,
                    content: content,
                    referenceId: evt.ReferenceId);

                await _notificationRepository.AddAsync(notification);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Invalidate unread count cache
                await _cacheService.RemoveAsync($"notifications:unread-count:{evt.UserId}", cancellationToken);

                // Push real-time via SignalR
                var dto = _mapper.Map<NotificationDto>(notification);
                await _hubService.SendNotificationAsync(evt.UserId, dto);

                // Fetch new unread count via domain abstraction
                var unreadCount = await _notificationRepository.CountUnreadAsync(evt.UserId, cancellationToken);

                // Push unread count to SignalR
                await _hubService.SendUnreadCountAsync(evt.UserId, unreadCount);

                _logger.LogInformation("Notification {Id} created and pushed for event {Type}. Unread count: {Count}", notification.Id, evt.Type, unreadCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create notification or push to Redis/SignalR for event {Type} -> User {UserId}", evt.Type, evt.UserId);
            }
        }
    }
}
