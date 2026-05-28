using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using static Vivu.Domain.Errors.DomainErrors;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.Interfaces.AI;
using Vivu.Application.UseCases.Notifications.Events;
using Vivu.Domain.Shared;
using Vivu.Infrastructure.Data;
using Vivu.Infrastructure.Repository;
using Vivu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Vivu.Domain.Interfaces;

namespace Vivu.Infrastructure.Services.AI
{
    public class RateLimitService :  IRateLimitService
    {
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;
        private readonly IApiUsageLogRepository _apiUsageLogRepository;
        private readonly IPublisher _publisher;
        private readonly ILogger<RateLimitService> _logger;

        public RateLimitService(
            IUserSubscriptionRepository userSubscriptionRepository,
            IApiUsageLogRepository apiUsageLogRepository,
            IPublisher publisher,
            ILogger<RateLimitService> logger) 
        {
            _userSubscriptionRepository = userSubscriptionRepository;
            _apiUsageLogRepository = apiUsageLogRepository;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task<Result<RateLimitCheckResult>> CheckLimitAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var subscription = await _userSubscriptionRepository.GetUserActiveSubscription(userId, cancellationToken);

                var dailyLimit = subscription?.Package?.MaxAiRequestPerDay ?? 5;

                var startOfDay = DateTime.UtcNow.Date;
                var endOfDay = startOfDay.AddDays(1);

                var currentCount = await _apiUsageLogRepository.GetUserUsageWithDateRange(startOfDay, endOfDay, userId, cancellationToken);

                var remaining = Math.Max(0, dailyLimit - currentCount);
                var resetAt = DateTime.UtcNow.Date.AddDays(1);

                _logger.LogDebug(
                    "Rate limit check for user {UserId}: {Current}/{Limit} (Remaining: {Remaining})",
                    userId, currentCount, dailyLimit, remaining);

                if (currentCount >= dailyLimit)
                {
                    // Publish quota exceeded event
                    await _publisher.Publish(new AIQuotaExceededEvent(userId, dailyLimit), cancellationToken);
                    return Result<RateLimitCheckResult>.Failure(AIErrors.RateLimitExceeded(remaining, resetAt));
                }

                // Check if approaching 80% threshold
                var threshold = (int)Math.Ceiling(dailyLimit * 0.8);
                if (currentCount == threshold)
                {
                    await _publisher.Publish(new AIQuotaLowEvent(userId, remaining, dailyLimit), cancellationToken);
                }

                return new RateLimitCheckResult
                {
                    Remaining = remaining,
                    Limit = dailyLimit,
                    ResetAt = resetAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking rate limit for user {UserId}", userId);
                throw;
            }
        }
    }
}
