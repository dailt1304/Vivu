using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Statistics;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Statictis.Queries.GetAdminDashboard;

public class GetAdminDashboardQueryHandler
    : IRequestHandler<GetAdminDashboardQuery, Result<AdminDashboardDto>>
{
    private readonly IUserRepository _userRepo;
    private readonly IUserSubscriptionRepository _subRepo;
    private readonly ITransactionRepository _txRepo;
    private readonly IBlogRepository _blogRepo;
    private readonly IBlogReportRepository _blogReportRepo;
    private readonly ITripRepository _tripRepo;
    private readonly ILocationRepository _locationRepo;
    private readonly ILocationReportRepository _locationReportRepo;
    private readonly ILogger<GetAdminDashboardQueryHandler> _logger;

    public GetAdminDashboardQueryHandler(
        IUserRepository userRepo,
        IUserSubscriptionRepository subRepo,
        ITransactionRepository txRepo,
        IBlogRepository blogRepo,
        IBlogReportRepository blogReportRepo,
        ITripRepository tripRepo,
        ILocationRepository locationRepo,
        ILocationReportRepository locationReportRepo,
        ILogger<GetAdminDashboardQueryHandler> logger)
    {
        _userRepo = userRepo;
        _subRepo = subRepo;
        _txRepo = txRepo;
        _blogRepo = blogRepo;
        _blogReportRepo = blogReportRepo;
        _tripRepo = tripRepo;
        _locationRepo = locationRepo;
        _locationReportRepo = locationReportRepo;
        _logger = logger;
    }

    public async Task<Result<AdminDashboardDto>> Handle(
        GetAdminDashboardQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching admin dashboard statistics");

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var weekStart = now.AddDays(-(int)now.DayOfWeek);
        var prevMonthStart = monthStart.AddMonths(-1);
        var prevMonthEnd = monthStart.AddSeconds(-1);

        // ── Run queries sequentially (DbContext is NOT thread-safe) ──

        // User stats
        var totalUsers = await _userRepo.GetTotalCountAsync(cancellationToken);
        var newUsersMonth = await _userRepo.GetNewUsersCountAsync(monthStart, cancellationToken);
        var newUsersWeek = await _userRepo.GetNewUsersCountAsync(weekStart, cancellationToken);
        var activeSubs = await _subRepo.GetActiveSubscriptionsCountAsync(cancellationToken);
        var expiringSubs = await _subRepo.GetExpiringSubscriptionsCountAsync(7, cancellationToken);

        // Revenue stats
        var revenueMTD = await _txRepo.GetRevenueInPeriodAsync(monthStart, now, cancellationToken);
        var revenuePrev = await _txRepo.GetRevenueInPeriodAsync(prevMonthStart, prevMonthEnd, cancellationToken);
        var successTx = await _txRepo.GetTransactionCountByStatusAsync("Paid", monthStart, now, cancellationToken);
        var pendingTx = await _txRepo.GetTransactionCountByStatusAsync("Pending", monthStart, now, cancellationToken);

        // Content stats
        var totalBlogs = await _blogRepo.GetTotalCountAsync(cancellationToken);
        var pendingBlogs = await _blogRepo.GetCountByStatusAsync("pending", cancellationToken);
        var publishedBlogs = await _blogRepo.GetCountByStatusAsync("published", cancellationToken);
        var blogReports = await _blogReportRepo.GetPendingReportCountAsync(cancellationToken);
        var totalTrips = await _tripRepo.GetTotalCountAsync(cancellationToken);

        // Location stats
        var locationsByCat = await _locationRepo.GetLocationsByCategoryAsync(cancellationToken);
        var pendingLoc = await _locationRepo.GetPendingSubmissionsCountAsync(cancellationToken);
        var reportsByType = await _locationReportRepo.GetReportsByTypeAsync(cancellationToken);
        var verifyRate = await _locationRepo.GetVerificationRateAsync(cancellationToken);
        var totalVerified = await _locationRepo.GetTotalVerifiedLocationsAsync(cancellationToken);

        var dto = new AdminDashboardDto
        {
            Users = new UserStatsDto
            {
                TotalUsers = totalUsers,
                NewUsersThisMonth = newUsersMonth,
                NewUsersThisWeek = newUsersWeek,
                ActiveSubscriptions = activeSubs,
                ExpiringSubscriptions = expiringSubs
            },
            Revenue = new RevenueStatsDto
            {
                RevenueMTD = revenueMTD,
                RevenuePreviousMonth = revenuePrev,
                SuccessfulTransactions = successTx,
                PendingTransactions = pendingTx
            },
            Content = new ContentStatsDto
            {
                TotalBlogs = totalBlogs,
                PendingBlogs = pendingBlogs,
                PublishedBlogs = publishedBlogs,
                PendingBlogReports = blogReports,
                TotalTrips = totalTrips
            },
            Locations = new LocationStatsDto
            {
                LocationsByCategory = locationsByCat,
                PendingSubmissionsCount = pendingLoc,
                ReportsByType = reportsByType,
                VerificationRate = verifyRate,
                TotalVerifiedLocations = totalVerified
            }
        };

        _logger.LogInformation(
            "Dashboard stats: {Users} users, {Subs} subs, {Revenue} revenue, {Blogs} blogs, {Trips} trips, {PendingLoc} pending locations",
            dto.Users.TotalUsers, dto.Users.ActiveSubscriptions,
            dto.Revenue.RevenueMTD, dto.Content.TotalBlogs,
            dto.Content.TotalTrips, dto.Locations.PendingSubmissionsCount);

        return Result<AdminDashboardDto>.Success(dto);
    }
}
