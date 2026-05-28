using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.DTOs.Responses.TripDay;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Commands.ApproveLocationSuggestion
{
    public class ApproveLocationSuggestionCommandHandler : IRequestHandler<ApproveLocationSuggestionCommand, Result<LocationDto>>
    {
        private readonly ILocationRepository _locationRepository;
        private readonly ILocationReportRepository _locationReportRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<ApproveLocationSuggestionCommandHandler> _logger;

        public ApproveLocationSuggestionCommandHandler(
            ILocationRepository locationRepository,
            ILocationReportRepository locationReportRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentUser currentUser,
            ILogger<ApproveLocationSuggestionCommandHandler> logger)
        {
            _locationRepository = locationRepository;
            _locationReportRepository = locationReportRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Result<LocationDto>> Handle(ApproveLocationSuggestionCommand request, CancellationToken cancellationToken)
        {
            // Auth
            if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var currentUserId))
            {
                _logger.LogWarning("Approve Location Suggestion failed: user not authenticated/invalid id. TraceId: {TraceId}", _currentUser.TraceId);
                return Result<LocationDto>.Failure(DomainErrors.Auth.InvalidToken);
            }

            _logger.LogInformation("Approve Location Suggestion. UserId: {UserId}, Location: {LocationId}", currentUserId, request.LocationId);

            // Check Location exists
            var location = await _locationRepository.GetByIdWithDetailsAsync(request.LocationId, cancellationToken);
            if (location == null)
            {
                _logger.LogWarning("Location not found. LocationId: {LocationId}", request.LocationId);
                return Result<LocationDto>.Failure(DomainErrors.Location.NotFoundById(request.LocationId));
            }

            // Find pending report for this location
            var pendingReport = await _locationReportRepository
                .GetLocationReportsQuery(ReportStatus.PENDING.ToString(), ReportType.NEW_LOCATION.ToString())
                .Where(r => r.LocationId == request.LocationId)
                .FirstOrDefaultAsync(cancellationToken);

            if (pendingReport == null)
            {
                _logger.LogWarning(
                    "No pending NEW_LOCATION report found for location. LocationId: {LocationId}",
                    request.LocationId);
                return Result<LocationDto>.Failure(DomainErrors.Location.NoPendingReport);
            }

            // Update pending report to APPROVED
            pendingReport.UpdateStatus(ReportStatus.APPROVED.ToString(), request.AdminNote);
            _locationReportRepository.Update(pendingReport);

            // Set location as verified
            location.Update(isVerified: true);
            _locationRepository.Update(location);

            var saved = await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (saved <= 0)
            {
                _logger.LogError(
                    "Failed to save changes for location approval. LocationId: {LocationId}",
                    request.LocationId);
                return Result<LocationDto>.Failure(DomainErrors.Location.SaveFailed);
            }

            _logger.LogInformation(
                "Location suggestion approved successfully. LocationId: {LocationId}, ReportId: {ReportId}",
                request.LocationId,
                pendingReport.Id);

            // Reload location with updated data
            var updatedLocation = await _locationRepository.GetByIdWithDetailsAsync(request.LocationId, cancellationToken);
            var locationDto = _mapper.Map<LocationDto>(updatedLocation);

            return Result<LocationDto>.Success(locationDto);
        }
    }
}