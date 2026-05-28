using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Cache;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Notifications.Commands.MarkNotificationAsRead
{
    public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, Result<bool>>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly ICacheService _cacheService;
        private readonly ILogger<MarkNotificationAsReadCommandHandler> _logger;

        public MarkNotificationAsReadCommandHandler(
            INotificationRepository notificationRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            ICacheService cacheService,
            ILogger<MarkNotificationAsReadCommandHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Marking notification {Id} as read", request.NotificationId);

            var notification = await _notificationRepository.GetByIdAsync(request.NotificationId);
            if (notification == null)
            {
                return Result<bool>.Failure(DomainErrors.Notification.NotFoundById(request.NotificationId));
            }

            if (!Guid.TryParse(_currentUser.Id, out var userId) || notification.UserId != userId)
            {
                return Result<bool>.Failure(DomainErrors.Notification.AccessDenied);
            }

            if (notification.IsRead)
            {
                return Result<bool>.Success(true);
            }

            notification.IsRead = true;
            notification.ModifiedDate = DateTime.UtcNow;
            _notificationRepository.Update(notification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _cacheService.RemoveAsync($"notifications:unread-count:{userId}", cancellationToken);

            _logger.LogInformation("Notification {Id} marked as read", request.NotificationId);
            return Result<bool>.Success(true);
        }
    }
}
