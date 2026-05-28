using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Users.Queries.GetUsageStats
{
    public class GetUsageStatsQueryHandler
    : IRequestHandler<GetUsageStatsQuery, Result<UsageStatsDto>>
    {
        private readonly IApiUsageLogRepository _apiUsageLogRepository;
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;
        private readonly ICurrentUser _currentUser;

        public GetUsageStatsQueryHandler(
            IApiUsageLogRepository apiUsageLogRepository,
            IUserSubscriptionRepository userSubscriptionRepository,
            ICurrentUser currentUser)
        {
            _apiUsageLogRepository = apiUsageLogRepository;
            _userSubscriptionRepository = userSubscriptionRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<UsageStatsDto>> Handle(
            GetUsageStatsQuery request,
            CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
                return Result<UsageStatsDto>.Failure(DomainErrors.Auth.InvalidToken);

            var subscription = await _userSubscriptionRepository
                .GetUserActiveSubscription(userId, cancellationToken);

            var dailyLimit = subscription?.Package?.MaxAiRequestPerDay ?? 5;

            var startOfDay = DateTime.UtcNow.Date;
            var endOfDay = startOfDay.AddDays(1);

            var used = await _apiUsageLogRepository.GetUserUsageWithDateRange(
                startOfDay, endOfDay, userId, cancellationToken);

            return Result<UsageStatsDto>.Success(new UsageStatsDto
            {
                Used = used,
                Limit = dailyLimit,
                Remaining = Math.Max(0, dailyLimit - used),
                ResetAt = endOfDay,
                HasActiveSubscription = subscription != null,
                PackageName = subscription?.Package?.Name,
                SubscriptionEndDate = subscription?.EndDate
            });
        }
    }

}
