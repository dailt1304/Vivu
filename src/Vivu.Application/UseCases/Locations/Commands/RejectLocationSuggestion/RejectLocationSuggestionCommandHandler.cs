using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Commands.RejectLocationSuggestion
{
    public class RejectLocationSuggestionCommandHandler : IRequestHandler<RejectLocationSuggestionCommand, Result<LocationDto>>
    {
        private readonly ILocationReportRepository _locationReportRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<RejectLocationSuggestionCommandHandler> _logger;
        private readonly ICurrentUser _currentUser;

        public RejectLocationSuggestionCommandHandler(
            ILocationReportRepository locationReportRepository,
            ILocationRepository locationRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<RejectLocationSuggestionCommandHandler> logger,
            ICurrentUser currentUser)
        {
            _locationReportRepository = locationReportRepository;
            _locationRepository = locationRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _currentUser = currentUser;
        }

        public async Task<Result<LocationDto>> Handle(RejectLocationSuggestionCommand request, CancellationToken cancellationToken)
        {
            // Auth
            if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var currentUserId))
            {
                _logger.LogWarning("Request Location Suggestion failed: user not authenticated/invalid id. TraceId: {TraceId}", _currentUser.TraceId);
                return Result<LocationDto>.Failure(DomainErrors.Auth.InvalidToken);
            }

            _logger.LogInformation("Request Location Suggestion. UserId: {UserId}, Location: {LocationId}", currentUserId, request.LocationId);

            // Check location exist
            var location = await _locationRepository.GetByIdWithDetailsAsync(request.LocationId, cancellationToken);
            if (location == null)
            {
                _logger.LogWarning("Location not found. LocationId: {LocationId}", request.LocationId);
                return Result<LocationDto>.Failure(DomainErrors.Location.NotFoundById(request.LocationId));
            }

            // Find pending report for this location
            var pendingReports = await _locationReportRepository
                .GetLocationReportsQuery(ReportStatus.PENDING.ToString(), ReportType.NEW_LOCATION.ToString())
                .Where(r => r.LocationId == request.LocationId)
                .FirstOrDefaultAsync(cancellationToken);

            if (pendingReports == null)
            {
                _logger.LogWarning("No pending location submission reports found for LocationId: {LocationId}", request.LocationId);
                return Result<LocationDto>.Failure(DomainErrors.LocationReport.NotFound);
            }

            // Update pending report to REJECTED
            pendingReports.UpdateStatus(ReportStatus.REJECTED.ToString(), request.AdminNote);
            _locationReportRepository.Update(pendingReports);

            var saved = await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (saved <= 0)
            {
                _logger.LogError("Failed to save changes when rejecting location suggestion. LocationId: {LocationId}", request.LocationId);
                return Result<LocationDto>.Failure(DomainErrors.Location.SaveFailed);
            }

            _logger.LogInformation(
                "Location suggestion approved successfully. LocationId: {LocationId}, ReportId: {ReportId}",
                request.LocationId,
                pendingReports.Id);
            
            var locationDto = _mapper.Map<LocationDto>(location);

            return Result<LocationDto>.Success(locationDto);
        }
    }
}