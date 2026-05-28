using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.BlogReports;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.BlogReports.Commands.ReviewBlogReport;

public class ReviewBlogReportCommandHandler : IRequestHandler<ReviewBlogReportCommand, Result<ReviewBlogReportResponse>>
{
    private readonly IBlogReportRepository _blogReportRepository;
    private readonly IBlogRepository _blogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly ILogger<ReviewBlogReportCommandHandler> _logger;

    public ReviewBlogReportCommandHandler(
        IBlogReportRepository blogReportRepository,
        IBlogRepository blogRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IMapper mapper,
        ILogger<ReviewBlogReportCommandHandler> logger)
    {
        _blogReportRepository = blogReportRepository;
        _blogRepository = blogRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<ReviewBlogReportResponse>> Handle(ReviewBlogReportCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var report = await _blogReportRepository.GetReportByIdWithDetailsAsync(request.ReportId, cancellationToken);
            if (report == null)
            {
                return Result<ReviewBlogReportResponse>.Failure(
                    DomainErrors.BlogReport.NotFoundById(request.ReportId));
            }

            if (report.Status != "PENDING")
            {
                return Result<ReviewBlogReportResponse>.Failure(
                    DomainErrors.BlogReport.AlreadyProcessed);
            }

            var statusString = request.Status.ToString();
            report.Status = statusString;
            report.AdminNote = request.AdminNote;
            report.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(_currentUser.Id) && Guid.TryParse(_currentUser.Id, out var adminId))
            {
                report.ReviewedBy = adminId;
            }

            // If report is approved, hide the blog by changing its status
            if (request.Status == ReportStatus.APPROVED && report.Blog != null)
            {
                _logger.LogInformation("Hiding blog {BlogId} due to approved report {ReportId}",
                    report.BlogId, report.Id);

                report.Blog.Status = "hidden";
                report.Blog.UpdatedAt = DateTime.UtcNow;
                _blogRepository.Update(report.Blog);

                _logger.LogInformation("Blog {BlogId} hidden successfully", report.BlogId);
            }

            _blogReportRepository.Update(report);
            var saved = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (saved <= 0)
            {
                return Result<ReviewBlogReportResponse>.Failure(
                    DomainErrors.BlogReport.UpdateFailed);
            }

            var response = new ReviewBlogReportResponse
            {
                Id = report.Id,
                BlogId = report.BlogId,
                BlogTitle = report.Blog?.Title ?? string.Empty,
                Status = report.Status,
                AdminNote = report.AdminNote,
                UpdatedAt = report.UpdatedAt ?? DateTime.UtcNow
            };

            _logger.LogInformation("Blog report {ReportId} reviewed successfully with status {Status}",
                report.Id, statusString);

            return Result<ReviewBlogReportResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while reviewing blog report {ReportId}", request.ReportId);
            return Result<ReviewBlogReportResponse>.Failure(
                DomainErrors.BlogReport.ReviewError);
        }
    }
}
