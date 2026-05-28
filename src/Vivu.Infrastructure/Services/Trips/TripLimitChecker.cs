using Microsoft.EntityFrameworkCore;
using Vivu.Application.Interfaces.Trips;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;

namespace Vivu.Infrastructure.Services.Trips
{
    public class TripLimitChecker : ITripLimitChecker
    {
        private readonly ITripRepository _tripRepository;
        private readonly IUserRepository _userRepository;
        private const int FreeTripLimit = 5;
        private const int PremiumTripLimit = int.MaxValue;

        public TripLimitChecker(ITripRepository tripRepository, IUserRepository userRepository)
        {
            _tripRepository = tripRepository;
            _userRepository = userRepository;
        }

        public async Task<bool> CanCreateTripAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            if (user.HasRole(Role.Names.Premium))
                return true;

            var tripCount = await GetTripCountAsync(userId, cancellationToken);
            
            return tripCount < FreeTripLimit;
        }

        public async Task<int> GetTripCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var userTripsQuery = _tripRepository.GetTripsByUserIdQuery(userId);
            return await userTripsQuery.CountAsync(cancellationToken);
        }

        public int GetTripLimit(bool isPremium)
        {
            return isPremium ? PremiumTripLimit : FreeTripLimit;
        }
    }
}
