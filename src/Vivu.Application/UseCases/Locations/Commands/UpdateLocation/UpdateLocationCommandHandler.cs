using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Locations;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Commands.UpdateLocation
{
    public class UpdateLocationCommandHandler : IRequestHandler<UpdateLocationCommand, Result<LocationDto>>
    {
        private readonly ILocationRepository _locationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUser _currentUser;
        private readonly IHelperLocation _helperLocation;
        private readonly ILogger<UpdateLocationCommandHandler> _logger;

        public UpdateLocationCommandHandler(
            ILocationRepository locationRepository,
            IUnitOfWork unitOfWork,
            IHelperLocation helperLocation,
            IMapper mapper,
            ICurrentUser currentUser,
            ILogger<UpdateLocationCommandHandler> logger)
        {
            _locationRepository = locationRepository;
            _unitOfWork = unitOfWork;
            _helperLocation = helperLocation;
            _mapper = mapper;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Result<LocationDto>> Handle(
            UpdateLocationCommand request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
            {
                _logger.LogWarning("Update location failed: Invalid or missing user ID");
                return Result<LocationDto>.Failure(DomainErrors.Auth.InvalidToken);
            }

            _logger.LogInformation(
                "Updating location {LocationId} by user {UserId}",
                request.LocationId,
                userId);

            var location = await _locationRepository.GetByIdWithDetailsAsync(request.LocationId, cancellationToken);

            if (location == null)
            {
                _logger.LogWarning("Location with ID {LocationId} not found", request.LocationId);
                return Result<LocationDto>.Failure(DomainErrors.Location.NotFoundById(request.LocationId));
            }

            // Normalize empty strings to null
            var openingHours = _helperLocation.NullIfEmpty(request.OpeningHours);
            var phone = _helperLocation.NullIfEmpty(request.Phone);
            var website = _helperLocation.NullIfEmpty(request.Website);
            var tags = _helperLocation.NullIfEmpty(request.Tags);

            // Convert comma-separated images string to JSON array for jsonb column
            var imagesJson = _helperLocation.ConvertImagesToJson(request.Images);

            location.Update(
                name: request.Name,
                description: request.Description,
                address: request.Address,
                latitude: request.Latitude,
                longitude: request.Longitude,
                categoryId: request.CategoryId,
                images: imagesJson,
                isVerified: request.IsVerified);

            if (location.LocationDetail != null)
            {
                location.LocationDetail.Update(
                    openingHours: openingHours,
                    phone: phone,
                    website: website,
                    tags: tags,
                    images: imagesJson);
            }
            else if (openingHours != null || phone != null ||
                     website != null || tags != null || imagesJson != null)
            {
                location.LocationDetail = LocationDetail.Create(
                    locationId: location.Id,
                    openingHours: openingHours,
                    phone: phone,
                    website: website,
                    tags: tags,
                    images: imagesJson);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully updated location {LocationId} by user {UserId}",
                request.LocationId,
                userId);

            var locationDto = _mapper.Map<LocationDto>(location);
            return Result<LocationDto>.Success(locationDto);
        }
    }
}
