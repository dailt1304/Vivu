using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Files;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Commands.UpdateLocationSuggestion
{
    public class UpdateLocationSuggestionCommandHandler : IRequestHandler<UpdateLocationSuggestionCommand, Result<LocationDto>>
    {
        private readonly ILocationRepository _locationRepository;
        private readonly ILocationReportRepository _locationReportRepository;
        private readonly ICityRepository _cityRepository;
        private readonly ILocationCategoryRepository _locationCategoryRepository;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateLocationSuggestionCommandHandler> _logger;

        public UpdateLocationSuggestionCommandHandler(
            ILocationRepository locationRepository,
            ILocationReportRepository locationReportRepository,
            ICityRepository cityRepository,
            ILocationCategoryRepository locationCategoryRepository,
            ICloudinaryService cloudinaryService,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            IMapper mapper,
            ILogger<UpdateLocationSuggestionCommandHandler> logger)
        {
            _locationRepository = locationRepository;
            _locationReportRepository = locationReportRepository;
            _cityRepository = cityRepository;
            _locationCategoryRepository = locationCategoryRepository;
            _cloudinaryService = cloudinaryService;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<LocationDto>> Handle(UpdateLocationSuggestionCommand request, CancellationToken cancellationToken)
        {
            // Auth
            if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var currentUserId))
            {
                _logger.LogWarning("Update Location Suggestion failed: user not authenticated/invalid id. TraceId: {TraceId}", _currentUser.TraceId);
                return Result<LocationDto>.Failure(DomainErrors.Auth.InvalidToken);
            }

            _logger.LogInformation("Update Location Suggestion started for LocationId: {LocationId} by UserId: {UserId}. TraceId: {TraceId}",
                request.LocationId, currentUserId, _currentUser.TraceId);

            // Check if location exists
            var location = await _locationRepository.GetByIdWithDetailsAsync(request.LocationId, cancellationToken);
            if (location == null)
            {
                _logger.LogWarning("Update Location Suggestion failed: location not found. LocationId: {LocationId}. TraceId: {TraceId}",
                    request.LocationId, _currentUser.TraceId);
                return Result<LocationDto>.Failure(DomainErrors.Location.NotFoundById(request.LocationId));
            }

            // Get pending report for this location
            var pendingReport = await _locationReportRepository
                .GetLocationReportsQuery(status: ReportStatus.PENDING.ToString(), reportType: ReportType.NEW_LOCATION.ToString())
                .FirstOrDefaultAsync(r => r.LocationId == request.LocationId, cancellationToken);

            if (pendingReport == null)
            {
                _logger.LogWarning("Update Location Suggestion failed: location does not have pending report. LocationId: {LocationId}. TraceId: {TraceId}",
                    request.LocationId, _currentUser.TraceId);
                return Result<LocationDto>.Failure(DomainErrors.Location.CannotUpdateApprovedLocation);
            }

            // Only creator can update
            if (pendingReport.UserId != currentUserId)
            {
                _logger.LogWarning("Update Location Suggestion failed: user is not the creator. LocationId: {LocationId}, UserId: {UserId}, CreatorId: {CreatorId}. TraceId: {TraceId}",
                    request.LocationId, currentUserId, pendingReport.UserId, _currentUser.TraceId);
                return Result<LocationDto>.Failure(DomainErrors.Location.UnauthorizedToUpdate);
            }

            // Validate input
            var name = request.Name.Trim();
            var address = request.Address?.Trim();
            var hasCoords = request.Latitude.HasValue && request.Longitude.HasValue;

            // Check duplicate location by name and address/coordinates (exclude current location)

            if (!string.IsNullOrWhiteSpace(address))
            {
                var existsByAddress = await _locationRepository
                    .ExistsByNameAndAddressAsync(name, address, request.LocationId, cancellationToken);

                if (existsByAddress)
                {
                    _logger.LogWarning("Update Location Suggestion failed: duplicate location by name and address. Name: {Name}, Address: {Address}. TraceId: {TraceId}",
                        name, address, _currentUser.TraceId);
                    return Result<LocationDto>.Failure(DomainErrors.Location.DuplicatedNameAndAddress);
                }
            }

            if (hasCoords)
            {
                var existsByCoords = await _locationRepository
                    .ExistsByNameAndCoordinatesAsync(name, request.Latitude!.Value, request.Longitude!.Value, request.LocationId, cancellationToken);

                if (existsByCoords)
                {
                    _logger.LogWarning("Update Location Suggestion failed: duplicate location by name and coordinates. Name: {Name}, Lat: {Lat}, Lng: {Lng}. TraceId: {TraceId}",
                        name, request.Latitude, request.Longitude, _currentUser.TraceId);
                    return Result<LocationDto>.Failure(DomainErrors.Location.DuplicatedNameAndCoordinates);
                }
            }

            // Validate CategoryId if provided
            if (request.CategoryId.HasValue)
            {
                var categoryExists = await _locationCategoryRepository.GetByIdAsync(request.CategoryId.Value);
                if (categoryExists is null)
                {
                    _logger.LogWarning("Update Location Suggestion failed: location category not found. CategoryId: {CategoryId}. TraceId: {TraceId}",
                        request.CategoryId.Value, _currentUser.TraceId);
                    return Result<LocationDto>.Failure(DomainErrors.LocationCategory.NotFound);
                }
            }

            // Validate CityId if provided
            if (request.CityId.HasValue)
            {
                var cityExists = await _cityRepository.GetByIdAsync(request.CityId.Value);
                if (cityExists is null)
                {
                    _logger.LogWarning("Update Location Suggestion failed: city not found. CityId: {CityId}. TraceId: {TraceId}",
                        request.CityId.Value, _currentUser.TraceId);
                    return Result<LocationDto>.Failure(DomainErrors.Cities.NotFound);
                }
            }

            // Upload images (required)
            string imageUrlsJson;
            try
            {
                _logger.LogInformation("Uploading {Count} images to Cloudinary for LocationId: {LocationId}. TraceId: {TraceId}",
                    request.Images.Count, request.LocationId, _currentUser.TraceId);

                var imageUrls = new List<string>();
                foreach (var image in request.Images)
                {
                    using var stream = image.OpenReadStream();
                    var url = await _cloudinaryService.UploadImageAsync(
                        imageStream: stream,
                        fileName: image.FileName,
                        folder: $"locations/{request.LocationId}"
                    );
                    imageUrls.Add(url);
                }

                imageUrlsJson = JsonSerializer.Serialize(imageUrls);

                _logger.LogInformation("Successfully uploaded {Count} images to Cloudinary for LocationId: {LocationId}. TraceId: {TraceId}",
                    imageUrls.Count, request.LocationId, _currentUser.TraceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload images to Cloudinary for LocationId: {LocationId}. TraceId: {TraceId}",
                    request.LocationId, _currentUser.TraceId);
                return Result<LocationDto>.Failure(DomainErrors.Location.ImageUploadFailed);
            }

            // Update Location entity with new information
            location.Update(
                name: request.Name,
                description: request.Description,
                address: request.Address,
                latitude: request.Latitude,
                longitude: request.Longitude,
                categoryId: request.CategoryId,
                images: imageUrlsJson,
                isVerified: false);

            // Update or create LocationDetail
            if (location.LocationDetail != null)
            {
                location.LocationDetail.Update(
                    openingHours: request.OpeningHours,
                    phone: request.Phone,
                    website: request.Website,
                    tags: request.Tags,
                    images: imageUrlsJson);
            }
            else
            {
                location.LocationDetail = LocationDetail.Create(
                    locationId: request.LocationId,
                    openingHours: request.OpeningHours,
                    phone: request.Phone,
                    website: request.Website,
                    tags: request.Tags,
                    images: imageUrlsJson);
            }

            _logger.LogDebug("Location and LocationDetail updated. LocationId: {LocationId}. TraceId: {TraceId}",
                request.LocationId, _currentUser.TraceId);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,
                    "Update Location Suggestion DbUpdateException. Inner={Inner}. TraceId={TraceId}",
                    ex.InnerException?.Message,
                    _currentUser.TraceId);

                return Result<LocationDto>.Failure(DomainErrors.Location.SaveFailed);
            }

            _logger.LogInformation(
                "Location updated successfully. LocationId: {LocationId}, UserId: {UserId}. TraceId: {TraceId}",
                request.LocationId, currentUserId, _currentUser.TraceId);

            var locationDto = _mapper.Map<LocationDto>(location);
            return Result<LocationDto>.Success(locationDto);
        }
    }
}