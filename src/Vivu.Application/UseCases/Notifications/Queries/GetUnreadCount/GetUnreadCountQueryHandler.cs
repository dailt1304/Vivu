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

namespace Vivu.Application.UseCases.Notifications.Queries.GetUnreadCount
{
    public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, Result<int>>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ICurrentUser _currentUser;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetUnreadCountQueryHandler> _logger;

        public GetUnreadCountQueryHandler(
            INotificationRepository notificationRepository,
            ICurrentUser currentUser,
            ICacheService cacheService,
            ILogger<GetUnreadCountQueryHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _currentUser = currentUser;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<int>> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
            {
                return Result<int>.Failure(DomainErrors.Auth.InvalidToken);
            }

            var cacheKey = $"notifications:unread-count:{userId}";

            var cached = await _cacheService.GetAsync<int?>(cacheKey, cancellationToken);
            if (cached.HasValue)
            {
                _logger.LogDebug("Unread count cache hit for user {UserId}: {Count}", userId, cached.Value);
                return Result<int>.Success(cached.Value);
            }

            var count = await _notificationRepository
                .GetByUserIdQuery(userId)
                .Where(n => !n.IsRead)
                .CountAsync(cancellationToken);

            await _cacheService.SetAsync(cacheKey, count, TimeSpan.FromMinutes(1), cancellationToken);

            _logger.LogInformation("Unread count for user {UserId}: {Count}", userId, count);
            return Result<int>.Success(count);
        }
    }
}
