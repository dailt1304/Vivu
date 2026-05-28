using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Cache;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Notifications.Commands.MarkAllAsRead
{
    public class MarkAllAsReadCommandHandler : IRequestHandler<MarkAllAsReadCommand, Result<int>>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly ICacheService _cacheService;
        private readonly ILogger<MarkAllAsReadCommandHandler> _logger;

        public MarkAllAsReadCommandHandler(
            INotificationRepository notificationRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            ICacheService cacheService,
            ILogger<MarkAllAsReadCommandHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<int>> Handle(MarkAllAsReadCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
            {
                return Result<int>.Failure(DomainErrors.Auth.InvalidToken);
            }

            _logger.LogInformation("Marking all notifications as read for user {UserId}", userId);

            var unreadNotifications = await _notificationRepository
                .GetByUserIdQuery(userId, trackChanges: true)
                .Where(n => !n.IsRead)
                .ToListAsync(cancellationToken);

            if (!unreadNotifications.Any())
            {
                return Result<int>.Success(0);
            }

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ModifiedDate = DateTime.UtcNow;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _cacheService.RemoveAsync($"notifications:unread-count:{userId}", cancellationToken);

            _logger.LogInformation("Marked {Count} notifications as read for user {UserId}", unreadNotifications.Count, userId);
            return Result<int>.Success(unreadNotifications.Count);
        }
    }
}
