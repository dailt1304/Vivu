using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.UseCases.Notifications.Events;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Subscriptions.Commands.ExpireSubscriptions;

public class ExpireSubscriptionsCommandHandler : IRequestHandler<ExpireSubscriptionsCommand, Result>
{
    private readonly IUserSubscriptionRepository _userSubscriptionRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly ILogger<ExpireSubscriptionsCommandHandler> _logger;

    public ExpireSubscriptionsCommandHandler(
        IUserSubscriptionRepository userSubscriptionRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        ILogger<ExpireSubscriptionsCommandHandler> logger)
    {
        _userSubscriptionRepository = userSubscriptionRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result> Handle(ExpireSubscriptionsCommand request, CancellationToken cancellationToken)
    {
        // 1. Lấy tất cả subscription Status=Active nhưng EndDate đã qua
        var expiredSubscriptions = await _userSubscriptionRepository
            .GetExpiredActiveSubscriptionsAsync(cancellationToken);

        if (expiredSubscriptions.Count == 0)
        {
            _logger.LogInformation("No expired subscriptions found.");
            return Result.Success();
        }

        _logger.LogInformation(
            "Found {Count} expired subscription(s) to process.", expiredSubscriptions.Count);

        // 2. Lấy PREMIUM role
        var premiumRole = await _roleRepository.GetByNameAsync(Role.Names.Premium);
        if (premiumRole == null)
        {
            _logger.LogWarning("PREMIUM role not found in database. Cannot remove roles.");
        }

        // 3. Thu thập userIds để publish notification sau SaveChanges
        var expiredUserIds = new List<Guid>();

        // 4. Loop qua từng subscription
        foreach (var subscription in expiredSubscriptions)
        {
            // Đánh dấu subscription hết hạn
            subscription.Expire();

            _logger.LogInformation(
                "Expired subscription {SubscriptionId} for User {UserId}. EndDate was {EndDate}.",
                subscription.Id, subscription.UserId, subscription.EndDate);

            // Xóa PREMIUM role nếu user không còn subscription active nào khác
            if (premiumRole != null && subscription.User != null)
            {
                // Sau khi gọi Expire(), subscription này đã có Status=Expired
                // Kiểm tra xem user có subscription Active khác không
                var hasOtherActive = await _userSubscriptionRepository
                    .GetUserActiveSubscription(subscription.UserId, cancellationToken);

                if (hasOtherActive == null)
                {
                    // Không còn subscription active nào => xóa PREMIUM role
                    subscription.User.RemoveRole(premiumRole.Id);

                    _logger.LogInformation(
                        "Removed PREMIUM role from User {UserId} — no active subscriptions remaining.",
                        subscription.UserId);
                }
            }

            expiredUserIds.Add(subscription.UserId);
        }

        // 5. Batch save tất cả thay đổi (subscriptions + user roles)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully expired {Count} subscription(s) and saved to database.", expiredSubscriptions.Count);

        // 6. Publish notification cho từng user (sau SaveChanges để đảm bảo data consistency)
        foreach (var userId in expiredUserIds)
        {
            try
            {
                await _publisher.Publish(
                    new SubscriptionExpiredEvent(userId), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish SubscriptionExpiredEvent for User {UserId}. " +
                    "Subscription was still expired successfully.", userId);
            }
        }

        _logger.LogInformation(
            "Expire Subscriptions completed. Total processed: {Count}", expiredSubscriptions.Count);

        return Result.Success();
    }
}
