using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Commands.DeleteTrip
{
    public class DeleteTripCommandHandler : IRequestHandler<DeleteTripCommand, Result<bool>>
    {
        private readonly ITripRepository _tripRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<DeleteTripCommandHandler> _logger;

        public DeleteTripCommandHandler(
            ITripRepository tripRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            ILogger<DeleteTripCommandHandler> logger)
        {
            _tripRepository = tripRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(
            DeleteTripCommand request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
            {
                _logger.LogWarning("Delete trip failed: Invalid or missing user ID");
                return Result<bool>.Failure(DomainErrors.Auth.InvalidToken);
            }

            _logger.LogInformation(
                "Deleting trip {TripId} by user {UserId}",
                request.TripId,
                userId);

            var trip = await _tripRepository.GetByIdAsync(request.TripId);

            if (trip == null)
            {
                _logger.LogWarning("Trip with ID {TripId} not found", request.TripId);
                return Result<bool>.Failure(DomainErrors.Trip.NotFoundById(request.TripId));
            }

            // Permission check: Only owner can delete
            if (!trip.CanBeDeletedBy(userId))
            {
                _logger.LogWarning(
                    "User {UserId} attempted to delete trip {TripId} without permission (not owner)",
                    userId,
                    request.TripId);
                return Result<bool>.Failure(DomainErrors.Trip.AccessDenied);
            }

            trip.Delete();

            _tripRepository.Update(trip);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully deleted trip {TripId} by user {UserId}",
                request.TripId,
                userId);

            return Result<bool>.Success(true);
        }
    }
}
