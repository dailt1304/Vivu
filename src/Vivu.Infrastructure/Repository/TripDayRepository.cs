using System.Threading;
using Microsoft.EntityFrameworkCore;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;

namespace Vivu.Infrastructure.Repository
{
    public class TripDayRepository : GenericRepository<TripDay>, ITripDayRepository
    {
        public TripDayRepository(VivuDbContext context) : base(context)
        {
        }

        // For modification/deletion - track the entity, don't include navigation properties
        public override async Task<TripDay?> GetByIdAsync(Guid id)
        {
            return await _dbSet
                .FirstOrDefaultAsync(td => td.Id == id);
        }

        // For read operations - no tracking, include navigation properties
        public async Task<TripDay?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet.AsNoTracking()
                .Include(td => td.Trip)
                .Include(td => td.TripLocations)
                .FirstOrDefaultAsync(td => td.Id == id);
        }

        public async Task<int> GetMaxIndexByTripIdAsync(Guid tripId)
        {
            return await _dbSet.Where(td => td.TripId == tripId)
                         .MaxAsync(td => (int?)td.DayIndex) ?? 0;
        }

        public async Task ReorderDayIndexAfterUpdateAsync(Guid tripId, int deletedDayIndex)
        {
            var toReorder = await _dbSet.Where(td => td.TripId == tripId && td.DayIndex > deletedDayIndex)
                                        .ToListAsync();
            foreach (var tripDay in toReorder)
            {
                tripDay.DayIndex -= 1;
            }
        }

        public async Task<List<TripDay>> GetByTripIdAsync(Guid tripId)
        {
            return await _dbSet.AsNoTracking()
                .Where(td => td.TripId == tripId)
                .OrderBy(td => td.DayIndex)
                .ToListAsync();
        }

        public async Task<TripDay?> GetByTripIdAndDayIndexAsync(Guid tripId, int dayIndex, CancellationToken cancellationToken)
        {
            return await _dbSet
                .Include(d => d.TripLocations)
                .FirstOrDefaultAsync(d =>
                    d.TripId == tripId && d.DayIndex == dayIndex,
                    cancellationToken);
        }

        public async Task ReorderTripDaysAsync(Guid tripId)
        {
            var tripDays = await GetByTripIdAsync(tripId);
            if (!tripDays.Any()) return;

            var ordered = tripDays
                .OrderBy(d => d.DayDate ?? DateTime.MaxValue)
                .ThenBy(d => d.DayIndex)
                .ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].DayIndex = i + 1;
            }
        }

        public List<TripDay> ReorderTripDaysAsync(List<TripDay> tripDays)
        {

            var ordered = tripDays
                .OrderBy(d => d.DayDate ?? DateTime.MaxValue)
                .ThenBy(d => d.DayIndex)
                .ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].DayIndex = i + 1;
            }
            return ordered;
        }
    }
}
