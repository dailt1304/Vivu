using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Domain.Entities;

namespace Vivu.Domain.Interfaces
{
    public interface ITripLocationRepository : IGenericRepository<TripLocation>
    {
        Task<bool> ExistsTimeConflictAsync(Guid tripDayId, Guid locationId, TimeSpan start, TimeSpan end, Guid? excludeTripLocationId = null);
        Task ReorderAfterDeleteAsync(Guid tripDayId, int deletedOrderIndex);
        Task<List<TripLocation>> GetTripLocationsByTripDayIdAsync(Guid tripDayId);
        Task<TripLocation?> GetByIdWithDetailsAsync(Guid id);
        Task<bool> HasActiveTripLocationsAsync(Guid locationId, CancellationToken cancellationToken = default);
        Task<TripLocation?> GetByLocationIdAndDayIndexAsync(Guid tripId, int dayIndex, Guid locationId, CancellationToken cancellationToken);
        Task ReorderTripLocationsAsync(Guid tripDayId);
        List<TripLocation> ReorderTripLocationsAsync(List<TripLocation> locations);
    }
}
