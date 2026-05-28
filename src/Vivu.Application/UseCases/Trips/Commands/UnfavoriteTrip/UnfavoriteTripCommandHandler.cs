using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Trips.Commands.FavouriteTrip;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Commands.UnfavoriteTrip
{
    public class UnfavoriteTripCommandHandler : IRequestHandler<UnfavoriteTripCommand, Result<Unit>>
    {
        private readonly ICurrentUser _currentUser;
        private readonly ITripRepository _tripRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITripFavoriteRepository _favoriteRepository;
        private readonly ILogger<UnfavoriteTripCommandHandler> _logger;

        public UnfavoriteTripCommandHandler(
            ILogger<UnfavoriteTripCommandHandler> logger,
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
            UnfavoriteTripCommand request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
            {
                _logger.LogWarning("Create trip failed: Invalid or missing user ID");
                return Result<Unit>.Failure(DomainErrors.Auth.InvalidToken);
            }

            var tripFavorite = await _favoriteRepository.GetTripFavouriteById(userId, request.TripId, cancellationToken);

            if (tripFavorite == null)
            {
                return Result<Unit>.Failure(DomainErrors.Trip.NotFavorited);
            }

            _favoriteRepository.Remove(tripFavorite);

            //Decrement denormalized counter 
            // var trip = await _tripRepository.FindAsync(request.TripId);
            // if (trip != null)
            // {
            //     trip.SaveCount--;
            //     _context.Trips.Update(trip);
            // }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
