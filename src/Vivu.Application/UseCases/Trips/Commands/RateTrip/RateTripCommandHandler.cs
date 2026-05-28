using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Commands.RateTrip
{
    public class RateTripCommandHandler : IRequestHandler<RateTripCommand, Result<bool>>
    {
        private readonly ITripRepository _tripRepository;
        private readonly ITripRatingRepository _tripRatingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<RateTripCommandHandler> _logger;

        public RateTripCommandHandler(
            ITripRepository tripRepository,
            ITripRatingRepository tripRatingRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            ILogger<RateTripCommandHandler> logger)
        {
            _tripRepository = tripRepository;
            _tripRatingRepository = tripRatingRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(RateTripCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Rate trip attempt. TripId: {TripId}, Rating: {Rating}",
                request.TripId,
                request.Rating);

            if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
            {
                _logger.LogWarning("Rate trip failed: Invalid or missing user ID");
                return Result<bool>.Failure(DomainErrors.Auth.InvalidToken);
            }

            _logger.LogDebug("User authenticated. UserId: {UserId}", userId);

            var trip = await _tripRepository.GetTripWithRatingAsync(request.TripId, cancellationToken);

            if (trip == null)
            {
                _logger.LogWarning("Rate trip failed: Trip not found. TripId: {TripId}", request.TripId);
                return Result<bool>.Failure(DomainErrors.Trip.NotFoundById(request.TripId));
            }

            // Validate user is trip owner
            if (trip.UserId != userId)
            {
                _logger.LogWarning(
                    "Rate trip failed: User is not trip owner. TripId: {TripId}, UserId: {UserId}, OwnerId: {OwnerId}",
                    request.TripId,
                    userId,
                    trip.UserId);
                return Result<bool>.Failure(DomainErrors.Trip.NotOwner);
            }

            // Validate trip is completed
            if (trip.Status.ToLower() != "completed")
            {
                _logger.LogWarning(
                    "Rate trip failed: Trip is not completed. TripId: {TripId}, Status: {Status}",
                    request.TripId,
                    trip.Status);
                return Result<bool>.Failure(DomainErrors.Trip.NotCompleted);
            }

            // Check if trip already has a rating
            if (trip.TripRating != null)
            {
                _logger.LogWarning(
                    "Rate trip failed: Trip already rated. TripId: {TripId}",
                    request.TripId);
                return Result<bool>.Failure(DomainErrors.Trip.AlreadyRated);
            }

            var tripRating = TripRating.Create(
                request.TripId,
                userId,
                request.Rating,
                request.ReviewContent);

            await _tripRatingRepository.AddAsync(tripRating);
            _logger.LogDebug("Trip rating entity created. TripRatingId: {TripRatingId}", tripRating.Id);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Trip rating saved to database. TripRatingId: {TripRatingId}", tripRating.Id);

            _logger.LogInformation(
                "Trip rated successfully. TripId: {TripId}, Rating: {Rating}, UserId: {UserId}",
                request.TripId,
                request.Rating,
                userId);

            return Result<bool>.Success(true);
        }
    }
}
