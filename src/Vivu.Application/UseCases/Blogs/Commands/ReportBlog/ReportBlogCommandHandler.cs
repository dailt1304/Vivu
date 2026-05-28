using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.BlogReports;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Commands.ReportBlog;

public class ReportBlogCommandHandler : IRequestHandler<ReportBlogCommand, Result<ReportBlogResponse>>
{
    private const int MaxReportsPerDay = 3;

    private readonly IBlogRepository _blogRepository;
    private readonly IBlogReportRepository _blogReportRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly ILogger<ReportBlogCommandHandler> _logger;

    public ReportBlogCommandHandler(
        IBlogRepository blogRepository,
        IBlogReportRepository blogReportRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IMapper mapper,
        ILogger<ReportBlogCommandHandler> logger)
    {
        _blogRepository = blogRepository;
        _blogReportRepository = blogReportRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<ReportBlogResponse>> Handle(ReportBlogCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Report blog attempt. BlogId: {BlogId}, ReportType: {ReportType}",
            request.BlogId,
            request.ReportType);

        if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            _logger.LogWarning("Report blog failed: Invalid or missing user ID");
            return Result<ReportBlogResponse>.Failure(DomainErrors.Auth.InvalidToken);
        }

        _logger.LogDebug("User authenticated. UserId: {UserId}", userId);

        var blog = await _blogRepository.GetByIdAsync(request.BlogId);
        if (blog == null)
        {
            _logger.LogWarning("Report blog failed: Blog not found. BlogId: {BlogId}", request.BlogId);
            return Result<ReportBlogResponse>.Failure(DomainErrors.Blog.NotFound);
        }

        var reportCountToday = await _blogReportRepository.GetUserReportCountTodayAsync(userId, cancellationToken);
        if (reportCountToday >= MaxReportsPerDay)
        {
            _logger.LogWarning(
                "Report blog failed: Rate limit exceeded. UserId: {UserId}, ReportCountToday: {ReportCount}",
                userId,
                reportCountToday);
            return Result<ReportBlogResponse>.Failure(DomainErrors.BlogReport.RateLimitExceeded);
        }

        var hasReported = await _blogReportRepository.HasUserReportedBlogAsync(userId, request.BlogId, cancellationToken);
        if (hasReported)
        {
            _logger.LogWarning(
                "Report blog failed: User already reported this blog. UserId: {UserId}, BlogId: {BlogId}",
                userId,
                request.BlogId);
            return Result<ReportBlogResponse>.Failure(DomainErrors.BlogReport.AlreadyReported);
        }

        var blogReport = BlogReport.Create(
            request.BlogId,
            userId,
            request.ReportType.ToString(),
            request.Reason,
            request.Description);

        await _blogReportRepository.AddAsync(blogReport);
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (result == 0)
        {
            _logger.LogError("Report blog failed: Unable to save changes to database");
            return Result<ReportBlogResponse>.Failure(DomainErrors.BlogReport.SaveFailed);
        }

        _logger.LogInformation(
            "Blog reported successfully. ReportId: {ReportId}, BlogId: {BlogId}, UserId: {UserId}, ReportType: {ReportType}",
            blogReport.Id,
            request.BlogId,
            userId,
            request.ReportType);

        var response = _mapper.Map<ReportBlogResponse>(blogReport);

        return Result<ReportBlogResponse>.Success(response);
    }
}
