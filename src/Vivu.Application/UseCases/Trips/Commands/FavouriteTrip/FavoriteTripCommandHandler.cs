using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Commands.FavouriteTrip
{
    public class FavoriteTripCommandHandler : IRequestHandler<FavoriteTripCommand, Result<Unit>>
    {
        private readonly ICurrentUser _currentUser;
        private readonly ITripRepository _tripRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITripFavoriteRepository _favoriteRepository;
        private readonly ILogger<FavoriteTripCommandHandler> _logger;

        public FavoriteTripCommandHandler(
            ILogger<FavoriteTripCommandHandler> logger,
            ITripRepository tripRepository,
            IUnitOfWork unitOfWork,
            ITripFavoriteRepository favoriteRepository,
            ICurrentUser currentUser)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _favoriteRepository = favoriteRepository;
            _tripRepository = tripRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<Unit>> Handle(
            FavoriteTripCommand request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
            {
                _logger.LogWarning("Create trip failed: Invalid or missing user ID");
                return Result<Unit>.Failure(DomainErrors.Auth.InvalidToken);
            }

            var trip = await _tripRepository.GetByIdAsync(request.TripId);

            if (trip == null)
            {
                _logger.LogWarning("Trip cannot be found with id {id}",request.TripId);
                return Result<Unit>.Failure(DomainErrors.Trip.NotFound);
            }
            _logger.LogDebug("Trip {id} has been found", trip.Id);

            if (!trip.IsPublic)
            {
                _logger.LogWarning("This trip is private so cannot favourtie");
                return Result<Unit>.Failure(DomainErrors.Trip.NotPublic);
            }

            var existingFavorite = await _favoriteRepository.GetTripFavouriteById(userId, request.TripId, cancellationToken);

            if (existingFavorite != null)
            {
                _logger.LogDebug("You have already favourite this trip");
                return Result<Unit>.Failure(DomainErrors.Trip.AlreadyFavorited);
            }

            var tripFavorite = new TripFavorite
            {
                UserId = userId,
                TripId = request.TripId,
                CreatedAt = DateTime.UtcNow
            };
            _logger.LogDebug("Trip favourite was created with user {userid} and trip {tripid}", userId, request.TripId);

            await _favoriteRepository.AddAsync(tripFavorite);

            //Increment denormalized counter 
            // trip.SaveCount++;
            // _context.Trips.Update(trip);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<Unit>.Success(Unit.Value);
        }
    }
}
