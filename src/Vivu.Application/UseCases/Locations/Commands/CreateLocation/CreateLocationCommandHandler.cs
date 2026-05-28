using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Files;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Commands.CreateLocation
{
    public class CreateLocationCommandHandler : IRequestHandler<CreateLocationCommand, Result<LocationDto>>
    {
        private readonly ILocationRepository _locationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly ICityRepository _cityRepository;
        private readonly ILocationCategoryRepository _locationCategoryRepository;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateLocationCommandHandler> _logger;

        public CreateLocationCommandHandler(
            ILocationRepository locationRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            ICityRepository cityRepository,
            ILocationCategoryRepository locationCategoryRepository,
            ICloudinaryService cloudinaryService,
            IMapper mapper,
            ILogger<CreateLocationCommandHandler> logger)
        {
            _locationRepository = locationRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _cityRepository = cityRepository;
            _locationCategoryRepository = locationCategoryRepository;
            _cloudinaryService = cloudinaryService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<LocationDto>> Handle(CreateLocationCommand request, CancellationToken cancellationToken)
        {
            // Auth
            if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var currentUserId))
            {
                _logger.LogWarning("Create Location failed: user not authenticated. TraceId: {TraceId}", _currentUser.TraceId);
                return Result<LocationDto>.Failure(DomainErrors.Auth.InvalidToken);
            }
            _logger.LogInformation("Create Location (Moderator) started by UserId: {UserId}. TraceId: {TraceId}", currentUserId, _currentUser.TraceId);

            var name = request.Name?.Trim();
            var address = request.Address?.Trim();

            // Validate city exists
            if (request.CityId.HasValue)
            {
                var cityExists = await _cityRepository.GetByIdAsync(request.CityId.Value);
                if (cityExists is null)
                {
                    _logger.LogWarning("Create Location failed: city not found. CityId: {CityId}. TraceId: {TraceId}", request.CityId.Value, _currentUser.TraceId);
                    return Result<LocationDto>.Failure(DomainErrors.Cities.NotFound);
                }
            }

            // Validate category exists
            if (request.CategoryId.HasValue)
            {
                var categoryExists = await _locationCategoryRepository.GetByIdAsync(request.CategoryId.Value);
                if (categoryExists is null)
                {
                    _logger.LogWarning("Create Location failed: category not found. CategoryId: {CategoryId}. TraceId: {TraceId}", request.CategoryId.Value, _currentUser.TraceId);
                    return Result<LocationDto>.Failure(DomainErrors.LocationCategory.NotFound);
                }
            }

            // Duplicate check by name + address
            if (!string.IsNullOrWhiteSpace(address))
            {
                var existsByAddress = await _locationRepository.ExistsByNameAndAddressAsync(name, address, cancellationToken: cancellationToken);
                if (existsByAddress)
                {
                    _logger.LogWarning("Create Location failed: duplicate by name+address. Name: {Name}, Address: {Address}. TraceId: {TraceId}", name, address, _currentUser.TraceId);
                    return Result<LocationDto>.Failure(DomainErrors.Location.DuplicatedNameAndAddress);
                }
            }

            // Duplicate check by name + coordinates
            var hasCoords = request.Latitude.HasValue && request.Longitude.HasValue;
            if (hasCoords)
            {
                var existsByCoords = await _locationRepository.ExistsByNameAndCoordinatesAsync(
                    name, request.Latitude!.Value, request.Longitude!.Value, cancellationToken: cancellationToken);
                if (existsByCoords)
                {
                    _logger.LogWarning("Create Location failed: duplicate by name+coords. Name: {Name}. TraceId: {TraceId}", name, _currentUser.TraceId);
                    return Result<LocationDto>.Failure(DomainErrors.Location.DuplicatedNameAndCoordinates);
                }
            }

            // Create Location entity — isVerified = true (Moderator-created)
            var location = Domain.Entities.Location.Create(
                name: name,
                description: request.Description,
                address: address,
                latitude: request.Latitude,
                longitude: request.Longitude,
                cityId: request.CityId,
                categoryId: request.CategoryId,
                isVerified: true);

            _logger.LogDebug("Location entity created. LocationId: {LocationId}. TraceId: {TraceId}", location.Id, _currentUser.TraceId);

            // Upload images to Cloudinary (if any)
            string imageUrlsJson = "[]";
            if (request.Images != null && request.Images.Any())
            {
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
                            folder: $"locations/{location.Id}");
                        imageUrls.Add(url);
                    }

                    imageUrlsJson = System.Text.Json.JsonSerializer.Serialize(imageUrls);
                    _logger.LogInformation("Successfully uploaded {Count} images. TraceId: {TraceId}", imageUrls.Count, _currentUser.TraceId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upload images for LocationId: {LocationId}. TraceId: {TraceId}", location.Id, _currentUser.TraceId);
                    return Result<LocationDto>.Failure(DomainErrors.Location.ImageUploadFailed);
                }
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

            // Persist
            try
            {
                await _locationRepository.AddAsync(location);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,
                    "CreateLocation DbUpdateException. Inner={Inner}. TraceId={TraceId}",
                    ex.InnerException?.Message, _currentUser.TraceId);
                return Result<LocationDto>.Failure(DomainErrors.Location.SaveFailed);
            }

            _logger.LogInformation(
                "Location created successfully (Moderator). LocationId: {LocationId}, Name: {Name}, UserId: {UserId}. TraceId: {TraceId}",
                location.Id, location.Name, currentUserId, _currentUser.TraceId);

            var locationDto = _mapper.Map<LocationDto>(location);
            return Result<LocationDto>.Success(locationDto);
        }
    }
}
