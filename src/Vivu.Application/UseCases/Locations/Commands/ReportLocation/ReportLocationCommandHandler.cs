using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.LocationReports;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;
using Vivu.Application.Interfaces.Files;

namespace Vivu.Application.UseCases.Locations.Commands.ReportLocation;

public class ReportLocationCommandHandler : IRequestHandler<ReportLocationCommand, Result<ReportLocationResponse>>
{
    private const int MaxReportsPerDay = 3;

    private readonly ILocationRepository _locationRepository;
    private readonly ILocationReportRepository _locationReportRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IMapper _mapper;
    private readonly ILogger<ReportLocationCommandHandler> _logger;

    public ReportLocationCommandHandler(
        ILocationRepository locationRepository,
        ILocationReportRepository locationReportRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ICloudinaryService cloudinaryService,
        IMapper mapper,
        ILogger<ReportLocationCommandHandler> logger)
    {
        _locationRepository = locationRepository;
        _locationReportRepository = locationReportRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _cloudinaryService = cloudinaryService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<ReportLocationResponse>> Handle(ReportLocationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Report location attempt. LocationId: {LocationId}, ReportType: {ReportType}",
            request.LocationId,
            request.ReportType);

        if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            _logger.LogWarning("Report location failed: Invalid or missing user ID");
            return Result<ReportLocationResponse>.Failure(DomainErrors.Auth.InvalidToken);
        }

        _logger.LogDebug("User authenticated. UserId: {UserId}", userId);

        var location = await _locationRepository.GetByIdAsync(request.LocationId);
        if (location == null)
        {
            _logger.LogWarning("Report location failed: Location not found. LocationId: {LocationId}", request.LocationId);
            return Result<ReportLocationResponse>.Failure(DomainErrors.Location.NotFoundById(request.LocationId));
        }

        var reportCountToday = await _locationReportRepository.GetUserReportCountTodayAsync(userId, cancellationToken);
        if (reportCountToday >= MaxReportsPerDay)
        {
            _logger.LogWarning(
                "Report location failed: Rate limit exceeded. UserId: {UserId}, ReportCountToday: {ReportCount}",
                userId,
                reportCountToday);
            return Result<ReportLocationResponse>.Failure(DomainErrors.LocationReport.RateLimitExceeded);
        }

        var hasReported = await _locationReportRepository.HasUserPendingReportForLocationAsync(userId, request.LocationId, cancellationToken);
        if (hasReported)
        {
            _logger.LogWarning(
                "Report location failed: User already has a pending report for this location. UserId: {UserId}, LocationId: {LocationId}",
                userId,
                request.LocationId);
            return Result<ReportLocationResponse>.Failure(DomainErrors.Location.AlreadyReported);
        }

        string? evidenceImagesJson = null;
        if (request.EvidenceImages?.Any() == true)
        {
            try
            {
                _logger.LogInformation("Uploading {Count} evidence images to Cloudinary for LocationId: {LocationId}", 
                    request.EvidenceImages.Count, request.LocationId);

                var imageUrls = new List<string>();
                foreach (var image in request.EvidenceImages)
                {
                    using var stream = image.OpenReadStream();
                    var url = await _cloudinaryService.UploadImageAsync(
                        imageStream: stream,
                        fileName: image.FileName,
                        folder: $"reports/{request.LocationId}"
                    );
                    imageUrls.Add(url);
                }

                evidenceImagesJson = System.Text.Json.JsonSerializer.Serialize(imageUrls);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload evidence images for report on LocationId: {LocationId}", request.LocationId);
                return Result<ReportLocationResponse>.Failure(DomainErrors.Location.ImageUploadFailed);
            }
        }

        var locationReport = LocationReport.Create(
            request.LocationId,
            userId,
            request.ReportType.ToString(),
            request.Reason,
            request.Description,
            request.SuggestedName,
            request.SuggestedAddress,
            evidenceImagesJson);

        await _locationReportRepository.AddAsync(locationReport);
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (result == 0)
        {
            _logger.LogError("Report location failed: Unable to save changes to database");
            return Result<ReportLocationResponse>.Failure(DomainErrors.LocationReport.SaveFailed);
        }

        var pendingReportCount = await _locationReportRepository.GetPendingReportCountForLocationAsync(
            request.LocationId, cancellationToken);
        var closedReportCount = await _locationReportRepository.GetClosedReportCountForLocationAsync(
            request.LocationId, cancellationToken);

        if ((pendingReportCount >= 5 || closedReportCount >= 5) && location.IsVerified)
        {
            location.Update(isVerified: false);
            _locationRepository.Update(location);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogWarning(
                "Location {LocationId} marked for review. PendingReports: {PendingCount}, ClosedReports: {ClosedCount}",
                request.LocationId, pendingReportCount, closedReportCount);
        }

        _logger.LogInformation(
            "Location reported successfully. ReportId: {ReportId}, LocationId: {LocationId}, UserId: {UserId}, ReportType: {ReportType}",
            locationReport.Id,
            request.LocationId,
            userId,
            request.ReportType);

        var response = _mapper.Map<ReportLocationResponse>(locationReport);

        return Result<ReportLocationResponse>.Success(response);
    }
}
