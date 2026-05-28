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

namespace Vivu.Application.UseCases.Notifications.Commands.DeleteNotification
{
    public class DeleteNotificationCommandHandler : IRequestHandler<DeleteNotificationCommand, Result<bool>>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly ICacheService _cacheService;
        private readonly ILogger<DeleteNotificationCommandHandler> _logger;

        public DeleteNotificationCommandHandler(
            INotificationRepository notificationRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            ICacheService cacheService,
            ILogger<DeleteNotificationCommandHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting notification {Id}", request.NotificationId);

            var notification = await _notificationRepository.GetByIdAsync(request.NotificationId);
            if (notification == null)
            {
                return Result<bool>.Failure(DomainErrors.Notification.NotFoundById(request.NotificationId));
            }

            if (!Guid.TryParse(_currentUser.Id, out var userId) || notification.UserId != userId)
            {
                return Result<bool>.Failure(DomainErrors.Notification.AccessDenied);
            }

            _notificationRepository.Remove(notification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (!notification.IsRead)
            {
                await _cacheService.RemoveAsync($"notifications:unread-count:{userId}", cancellationToken);
            }

            _logger.LogInformation("Notification {Id} deleted", request.NotificationId);
            return Result<bool>.Success(true);
        }
    }
}
