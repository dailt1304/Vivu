using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.BlogReports;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Commands.FlagBlog;

public class FlagBlogCommandHandler : IRequestHandler<FlagBlogCommand, Result<ReportBlogResponse>>
{

    private readonly IBlogRepository _blogRepository;
    private readonly IBlogReportRepository _blogReportRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly ILogger<FlagBlogCommandHandler> _logger;

    public FlagBlogCommandHandler(
        IBlogRepository blogRepository,
        IBlogReportRepository blogReportRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IMapper mapper,
        ILogger<FlagBlogCommandHandler> logger)
    {
        _blogRepository = blogRepository;
        _blogReportRepository = blogReportRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<ReportBlogResponse>> Handle(FlagBlogCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Flag blog attempt. BlogId: {BlogId}, ReportType: {ReportType}",
            request.BlogId,
            request.ReportType);

        if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            _logger.LogWarning("Flag blog failed: Invalid or missing user ID");
            return Result<ReportBlogResponse>.Failure(DomainErrors.Auth.InvalidToken);
        }

        var blog = await _blogRepository.GetByIdAsync(request.BlogId);
        if (blog == null)
        {
            _logger.LogWarning("Flag blog failed: Blog not found. BlogId: {BlogId}", request.BlogId);
            return Result<ReportBlogResponse>.Failure(DomainErrors.Blog.NotFound);
        }

        var hasFlagged = await _blogReportRepository.HasUserReportedBlogAsync(userId, request.BlogId, cancellationToken);
        if (hasFlagged)
        {
            _logger.LogWarning(
                "Flag blog failed: User already flagged this blog. UserId: {UserId}, BlogId: {BlogId}",
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
        var saved = await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (saved == 0)
        {
            _logger.LogError("Flag blog failed: Unable to save changes");
            return Result<ReportBlogResponse>.Failure(DomainErrors.BlogReport.SaveFailed);
        }

        _logger.LogInformation(
            "Blog flagged successfully. ReportId: {ReportId}, BlogId: {BlogId}, UserId: {UserId}",
            blogReport.Id,
            request.BlogId,
            userId);

        var response = _mapper.Map<ReportBlogResponse>(blogReport);
        return Result<ReportBlogResponse>.Success(response);
    }
}
