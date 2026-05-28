using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Commands.DeleteLocation
{
    public class DeleteLocationCommandHandler : IRequestHandler<DeleteLocationCommand, Result<bool>>
    {
        private readonly ILocationRepository _locationRepository;
        private readonly ITripLocationRepository _tripLocationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<DeleteLocationCommandHandler> _logger;

        public DeleteLocationCommandHandler(
            ILocationRepository locationRepository,
            ITripLocationRepository tripLocationRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            ILogger<DeleteLocationCommandHandler> logger)
        {
            _locationRepository = locationRepository;
            _tripLocationRepository = tripLocationRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(
            DeleteLocationCommand request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
            {
                _logger.LogWarning("Delete location failed: Invalid or missing user ID");
                return Result<bool>.Failure(DomainErrors.Auth.InvalidToken);
            }

            _logger.LogInformation(
                "Deleting location {LocationId} by user {UserId}",
                request.LocationId,
                userId);

            var location = await _locationRepository.GetByIdWithDetailsAsync(request.LocationId, cancellationToken);

            if (location == null)
            {
                _logger.LogWarning("Location with ID {LocationId} not found", request.LocationId);
                return Result<bool>.Failure(DomainErrors.Location.NotFoundById(request.LocationId));
            }

            // Check if location is already deleted
            if (location.IsDeleted)
            {
                _logger.LogWarning("Location with ID {LocationId} is already deleted", request.LocationId);
                return Result<bool>.Failure(DomainErrors.Location.AlreadyDeleted);
            }

            // Check if location is used in any active trips
            var hasActiveTrips = await _tripLocationRepository.HasActiveTripLocationsAsync(request.LocationId, cancellationToken);
            
            if (hasActiveTrips)
            {
                _logger.LogWarning(
                    "Cannot delete location {LocationId}: Location is being used in active trips",
                    request.LocationId);
                return Result<bool>.Failure(DomainErrors.Location.UsedInActiveTrips);
            }

            location.Delete();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully deleted location {LocationId} by user {UserId}",
                request.LocationId,
                userId);

            return Result<bool>.Success(true);
        }
    }
}
