using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Files;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Commands.SubmitNewLocation
{
    public class SubmitNewLocationCommandHandler : IRequestHandler<SubmitNewLocationCommand, Result<LocationDto>>
    {
        private const string SubmissionReportReason = "USER_SUBMISSION";

        private readonly ILocationRepository _locationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly ICityRepository _cityRepository;
        private readonly ILocationReportRepository _locationReportRepository;
        private readonly ILocationCategoryRepository _locationCategoryRepository;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMapper _mapper;
        private readonly ILogger<SubmitNewLocationCommandHandler> _logger;

        public SubmitNewLocationCommandHandler(
            ILocationRepository locationRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            ICityRepository cityRepository,
            ILocationReportRepository locationReportRepository,
            ILocationCategoryRepository locationCategoryRepository,
            ICloudinaryService cloudinaryService,
            IMapper mapper,
            ILogger<SubmitNewLocationCommandHandler> logger)
        {
            _locationRepository = locationRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _cityRepository = cityRepository;
            _locationReportRepository = locationReportRepository;
            _locationCategoryRepository = locationCategoryRepository;
            _cloudinaryService = cloudinaryService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<LocationDto>> Handle(SubmitNewLocationCommand request, CancellationToken cancellationToken)
        {
            // Auth
            if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var currentUserId))
            {
                _logger.LogWarning("Submit New Location failed: user not authenticated/invalid id. TraceId: {TraceId}", _currentUser.TraceId);
                return Result<LocationDto>.Failure(DomainErrors.Auth.InvalidToken);
            }
            _logger.LogInformation("Submit New Location started by UserId: {UserId}. TraceId: {TraceId}", currentUserId, _currentUser.TraceId);

            // Validate input
            var name = request.Name?.Trim();
            var address = request.Address?.Trim();
            var hasCoords = request.Latitude.HasValue && request.Longitude.HasValue;

            // Check location category exists
            if (request.CategoryId.HasValue)
            {
                var categoryExists = await _locationCategoryRepository.GetByIdAsync(request.CategoryId.Value);
                if (categoryExists is null)
                {
                    _logger.LogWarning("Submit New Location failed: location category not found. CategoryId: {CategoryId}. TraceId: {TraceId}", request.CategoryId.Value, _currentUser.TraceId);
                    return Result<LocationDto>.Failure(DomainErrors.LocationCategory.NotFound);
                }
            }

            // Check city exists
            if (request.CityId.HasValue)
            {
                var cityExists = await _cityRepository.GetByIdAsync(request.CityId.Value);
                if (cityExists is null)
                {
                    _logger.LogWarning("Submit New Location failed: city not found. CityId: {CityId}. TraceId: {TraceId}", request.CityId.Value, _currentUser.TraceId);
                    return Result<LocationDto>.Failure(DomainErrors.Cities.NotFound);
                }
            }

            // Check duplicate pending submission (anti-spam)
            var hasPendingSubmission = await _locationReportRepository.ExistsPendingNewLocationSubmissionAsync(currentUserId, name, address, request.Latitude, request.Longitude, cancellationToken);

            if (hasPendingSubmission)
            {
                _logger.LogWarning(
                    "Submit New Location failed: duplicate pending submission. UserId: {UserId}, Name: {Name}. TraceId: {TraceId}", currentUserId, name, _currentUser.TraceId);
                return Result<LocationDto>.Failure(DomainErrors.Location.DuplicatePendingSubmission);
            }

            // Check duplicate location by name and address/coordinates
            if (!string.IsNullOrWhiteSpace(address))
            {
                var existsByAddress = await _locationRepository.ExistsByNameAndAddressAsync(name, address, cancellationToken: cancellationToken);
                if (existsByAddress)
                {
                    _logger.LogWarning("Submit New Location failed: duplicate location by name and address. Name: {Name}, Address: {Address}. TraceId: {TraceId}", name, address, _currentUser.TraceId);
                    return Result<LocationDto>.Failure(DomainErrors.Location.DuplicatedNameAndAddress);
                }
            }
            if (hasCoords)
            {
                var existsByCoords = await _locationRepository.ExistsByNameAndCoordinatesAsync(name, request.Latitude!.Value, request.Longitude!.Value, cancellationToken: cancellationToken);

                if (existsByCoords)
                {
                    _logger.LogWarning(
                        "Submit New Location failed: duplicate location by name and coordinates. Name: {Name}, Lat: {Lat}, Lng: {Lng}. TraceId: {TraceId}", name, request.Latitude, request.Longitude, _currentUser.TraceId);
                    return Result<LocationDto>.Failure(DomainErrors.Location.DuplicatedNameAndCoordinates);
                }
            }

            // Create Location entity (pending verification)
            var location = Domain.Entities.Location.Create(
                name: name,
                description: request.Description,
                address: address,
                latitude: request.Latitude,
                longitude: request.Longitude,
                cityId: request.CityId,
                categoryId: request.CategoryId,
                isVerified: false);

            _logger.LogDebug("Location entity created. LocationId: {LocationId}. TraceId: {TraceId}", location.Id, _currentUser.TraceId);

            string imageUrlsJson;
            try
            {
                _logger.LogInformation("Uploading {Count} images to Cloudinary for LocationId: {LocationId}. TraceId: {TraceId}",
                    request.Images.Count, location.Id, _currentUser.TraceId);

                var imageUrls = new List<string>();
                foreach (var image in request.Images)
                {
                    using var stream = image.OpenReadStream();
                    var url = await _cloudinaryService.UploadImageAsync(
                        imageStream: stream,
                        fileName: image.FileName,
                        folder: $"locations/{location.Id}"
                    );
                    imageUrls.Add(url);
                }

                // Convert to JSON array for JSONB storage
                imageUrlsJson = System.Text.Json.JsonSerializer.Serialize(imageUrls);

                _logger.LogInformation("Successfully uploaded {Count} images to Cloudinary for LocationId: {LocationId}. TraceId: {TraceId}",
                    imageUrls.Count, location.Id, _currentUser.TraceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload images to Cloudinary for LocationId: {LocationId}. TraceId: {TraceId}",
                    location.Id, _currentUser.TraceId);
                return Result<LocationDto>.Failure(DomainErrors.Location.ImageUploadFailed);
            }

            // Create LocationDetail

            location.LocationDetail = LocationDetail.Create(
                locationId: location.Id,
                openingHours: request.OpeningHours,
                phone: request.Phone,
                website: request.Website,
                tags: request.Tags,
                images: imageUrlsJson);

            _logger.LogDebug("LocationDetail created for LocationId: {LocationId}. TraceId: {TraceId}", location.Id, _currentUser.TraceId);

            // Create LocationReport
            var report = LocationReport.Create(
                locationId: location.Id,
                userId: currentUserId,
                reportType: ReportType.NEW_LOCATION.ToString(),
                reportReason: SubmissionReportReason,
                reportDescription: request.Description);

            // Ensure pending status for moderator review
            report.Status = ReportStatus.PENDING.ToString();

            _logger.LogDebug("LocationReport created. ReportId: {ReportId}, LocationId: {LocationId}. TraceId: {TraceId}", report.Id, location.Id, _currentUser.TraceId);

            try
            {
                await _locationRepository.AddAsync(location);
                await _locationReportRepository.AddAsync(report);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,
                    "SubmitNewLocation DbUpdateException. Inner={Inner}. TraceId={TraceId}",
                    ex.InnerException?.Message,
                    _currentUser.TraceId);

                return Result<LocationDto>.Failure(DomainErrors.Location.SaveFailed);
            }

            _logger.LogInformation(
                "Location submitted successfully. LocationId: {LocationId}, ReportId: {ReportId}, Name: {Name}, UserId: {UserId}. TraceId: {TraceId}",
                location.Id, report.Id, location.Name, currentUserId, _currentUser.TraceId);

            var locationDto = _mapper.Map<LocationDto>(location);
            return Result<LocationDto>.Success(locationDto);
        }
    }
}