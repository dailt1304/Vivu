using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.LocationReports;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Files;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.LocationReports.Commands.UpdateLocationReport
{
    public class UpdateLocationReportCommandHandler : IRequestHandler<UpdateLocationReportCommand, Result<LocationReportDto>>
    {
        private readonly ILocationReportRepository _locationReportRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateLocationReportCommandHandler> _logger;

        public UpdateLocationReportCommandHandler(
            ILocationReportRepository locationReportRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            ICloudinaryService cloudinaryService,
            IMapper mapper,
            ILogger<UpdateLocationReportCommandHandler> logger)
        {
            _locationReportRepository = locationReportRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _cloudinaryService = cloudinaryService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<LocationReportDto>> Handle(UpdateLocationReportCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Update location report attempt. ReportId: {ReportId}", request.ReportId);

            if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
            {
                _logger.LogWarning("Update location report failed: Invalid or missing user ID");
                return Result<LocationReportDto>.Failure(DomainErrors.Auth.InvalidToken);
            }

            var report = await _locationReportRepository.GetByIdAsync(request.ReportId);
            if (report == null)
            {
                 _logger.LogWarning("Update location report failed: Location report not found. ReportId: {ReportId}", request.ReportId);
                 return Result<LocationReportDto>.Failure(DomainErrors.LocationReport.NotFoundById(request.ReportId));
            }

            if (report.UserId != userId)
            {
                 _logger.LogWarning("Update location report failed: User not owner. ReportId: {ReportId}, UserId: {UserId}", request.ReportId, userId);
                 return Result<LocationReportDto>.Failure(DomainErrors.LocationReport.NotReportOwner);
            }

            if (report.Status != "PENDING")
            {
                 _logger.LogWarning("Update location report failed: Report status is not PENDING. ReportId: {ReportId}, Status: {Status}", request.ReportId, report.Status);
                 return Result<LocationReportDto>.Failure(DomainErrors.LocationReport.CannotUpdateNonPending);
            }

            string? evidenceImagesJson = null;
            if (request.EvidenceImages?.Any() == true)
            {
                try
                {
                    _logger.LogInformation("Uploading {Count} new evidence images to Cloudinary for ReportId: {ReportId}", 
                        request.EvidenceImages.Count, request.ReportId);

                    var imageUrls = new List<string>();
                    foreach (var image in request.EvidenceImages)
                    {
                        using var stream = image.OpenReadStream();
                        var url = await _cloudinaryService.UploadImageAsync(
                            imageStream: stream,
                            fileName: image.FileName,
                            folder: $"reports/{report.LocationId}"
                        );
                        imageUrls.Add(url);
                    }

                    evidenceImagesJson = System.Text.Json.JsonSerializer.Serialize(imageUrls);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upload evidence images for ReportId: {ReportId}", request.ReportId);
                    return Result<LocationReportDto>.Failure(DomainErrors.Location.ImageUploadFailed);
                }
            }

            report.UpdateContent(
                request.ReportType,
                request.ReportReason,
                request.ReportDescription,
                request.SuggestedName,
                request.SuggestedAddress,
                evidenceImagesJson);

            _locationReportRepository.Update(report);
            var result = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (result == 0)
            {
                _logger.LogError("Update location report failed: Unable to save changes to database");
                return Result<LocationReportDto>.Failure(DomainErrors.LocationReport.SaveFailed); // Should add DomainErrors.LocationReport.SaveFailed? Actually let's use a generic save failed or just checking what usually returned.
            }

            _logger.LogInformation("Location report updated successfully. ReportId: {ReportId}", request.ReportId);

            var response = _mapper.Map<LocationReportDto>(report);

            return Result<LocationReportDto>.Success(response);
        }
    }
}
