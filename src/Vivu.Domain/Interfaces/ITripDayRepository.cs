using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vivu.Domain.Entities;

namespace Vivu.Domain.Interfaces
{
    public interface ITripDayRepository : IGenericRepository<TripDay>
    {
        Task<TripDay?> GetByIdWithDetailsAsync(Guid id);
        Task<int> GetMaxIndexByTripIdAsync (Guid tripId);
        Task ReorderDayIndexAfterUpdateAsync(Guid tripId, int deletedDayIndex);
        Task<List<TripDay>> GetByTripIdAsync(Guid tripId);
        Task<TripDay?> GetByTripIdAndDayIndexAsync(Guid TripId, int DayIndex, CancellationToken cancellationToken);
        Task ReorderTripDaysAsync(Guid tripId);
        List<TripDay> ReorderTripDaysAsync(List<TripDay> tripDays);
    }
}
