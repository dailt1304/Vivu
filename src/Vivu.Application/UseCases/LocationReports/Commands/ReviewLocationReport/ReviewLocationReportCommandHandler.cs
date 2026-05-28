using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.LocationReports;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.LocationReports.Commands.ReviewLocationReport;

public class ReviewLocationReportCommandHandler : IRequestHandler<ReviewLocationReportCommand, Result<ReviewLocationReportResponse>>
{
    private readonly ILocationReportRepository _locationReportRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly ILogger<ReviewLocationReportCommandHandler> _logger;

    public ReviewLocationReportCommandHandler(
        ILocationReportRepository locationReportRepository,
        ILocationRepository locationRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IMapper mapper,
        ILogger<ReviewLocationReportCommandHandler> logger)
    {
        _locationReportRepository = locationReportRepository;
        _locationRepository = locationRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<ReviewLocationReportResponse>> Handle(ReviewLocationReportCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var report = await _locationReportRepository.GetReportByIdWithDetailsAsync(request.ReportId, cancellationToken);
            if (report == null)
            {
                return Result<ReviewLocationReportResponse>.Failure(
                    DomainErrors.LocationReport.NotFoundById(request.ReportId));
            }

            if (report.Status != "PENDING")
            {
                return Result<ReviewLocationReportResponse>.Failure(
                    DomainErrors.LocationReport.AlreadyProcessed);
            }

            var statusString = request.Status.ToString();
            report.Status = statusString;
            report.AdminNote = request.AdminNote;
            report.UpdatedAt = DateTime.UtcNow;

            if (request.Status == ReportStatus.APPROVED && 
                report.ReportType == ReportType.WRONG_INFO.ToString())
            {
                if (report.Location != null)
                {
                    _logger.LogInformation("Updating location {LocationId} with approved suggestions from report {ReportId}", 
                        report.LocationId, report.Id);
                    
                    report.Location.Update(
                        name: request.Name,
                        description: request.Description,
                        images: request.Images,
                        isVerified: true
                    );
                    
                    _locationRepository.Update(report.Location);
                    
                    _logger.LogInformation("Location {LocationId} updated successfully", report.LocationId);
                }
            }

            if (request.Status == ReportStatus.APPROVED &&
                report.ReportType == ReportType.CLOSED.ToString())
            {
                if (report.Location != null)
                {
                    _logger.LogInformation(
                        "Soft-deleting location {LocationId} based on approved CLOSED report {ReportId}",
                        report.LocationId, report.Id);

                    report.Location.Delete();
                    _locationRepository.Update(report.Location);
                }
            }

            _locationReportRepository.Update(report);
            var saved = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (saved <= 0)
            {
                return Result<ReviewLocationReportResponse>.Failure(
                    DomainErrors.LocationReport.UpdateFailed);
            }

            var response = new ReviewLocationReportResponse
            {
                Id = report.Id,
                LocationId = report.LocationId,
                LocationName = report.Location?.Name ?? string.Empty,
                Status = report.Status,
                AdminNote = report.AdminNote,
                UpdatedAt = report.UpdatedAt ?? DateTime.UtcNow
            };

            _logger.LogInformation("Report {ReportId} reviewed successfully with status {Status}", 
                report.Id, statusString);

            return Result<ReviewLocationReportResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while reviewing report {ReportId}", request.ReportId);
            return Result<ReviewLocationReportResponse>.Failure(
                DomainErrors.LocationReport.ReviewError);
        }
    }
}
