using Microsoft.EntityFrameworkCore;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;

namespace Vivu.Infrastructure.Repository
{
    public class TripLocationRepository : GenericRepository<TripLocation>, ITripLocationRepository
    {
        public TripLocationRepository(VivuDbContext context) : base(context)
        {
        }

        public Task<bool> ExistsTimeConflictAsync(Guid tripDayId, Guid locationId, TimeSpan start, TimeSpan end, Guid? excludeTripLocationId = null)
        {
            return _dbSet.AnyAsync(x => x.TripDayId == tripDayId
                                  && x.LocationId == locationId
                                  && x.Id != excludeTripLocationId  // Exclude current TripLocation when updating
                                  && x.StartTime.HasValue
                                  && x.EndTime.HasValue
                                  && x.StartTime.Value < end
                                  && start < x.EndTime.Value);
        }

        public override async Task<TripLocation?> GetByIdAsync(Guid id)
        {
            return await _dbSet.AsNoTracking()
                .Include(tl => tl.TripDay)
                .Include(tl => tl.Location)
                .FirstOrDefaultAsync(tl => tl.Id == id);
        }

        public Task<List<TripLocation>> GetTripLocationsByTripDayIdAsync(Guid tripDayId)
        {
            return _dbSet.Include(tl => tl.Location)
                         .Where(tl => tl.TripDayId == tripDayId)
                         .OrderBy(tl => tl.OrderIndex)
                         .ToListAsync();
        }

        public async Task ReorderTripLocationsAsync(Guid tripDayId)
        {
            var locations = await GetTripLocationsByTripDayIdAsync(tripDayId);
            if (!locations.Any()) return;

            // Sort by StartTime (nulls last)
            var ordered = locations.OrderBy(l => l.StartTime == null).ThenBy(l => l.StartTime).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].OrderIndex = i + 1;
                _dbSet.Update(ordered[i]);
            }
        }

        public List<TripLocation> ReorderTripLocationsAsync(List<TripLocation> locations)
        {
            // Sort by StartTime (nulls last)
            var ordered = locations.OrderBy(l => l.StartTime == null).ThenBy(l => l.StartTime).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].OrderIndex = i + 1;
            }
            return ordered;
        }

        public async Task<TripLocation?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(tl => tl.TripDay)
                .Include(tl => tl.Location)
                    .ThenInclude(l => l.LocationDetail)
                .Include(tl => tl.Alternatives)
                    .ThenInclude(a => a.Location)
                        .ThenInclude(l => l.LocationDetail)
                .FirstOrDefaultAsync(tl => tl.Id == id);
        }

        public async Task ReorderAfterDeleteAsync(Guid tripDayId, int deletedOrderIndex)
        {
            var toReorder = await _dbSet.Where(x => x.TripDayId == tripDayId && x.OrderIndex > deletedOrderIndex)
                                        .OrderBy(x => x.OrderIndex)
                                        .ToListAsync();

            foreach (var item in toReorder)
                item.OrderIndex -= 1;
        }

        public async Task<bool> HasActiveTripLocationsAsync(Guid locationId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(tl => tl.TripDay)
                    .ThenInclude(td => td.Trip)
                .AnyAsync(tl => 
                    tl.LocationId == locationId && 
                    !tl.TripDay.Trip.IsDeleted && 
                    tl.TripDay.Trip.Status != "completed",
                    cancellationToken);
        }

        public async Task<TripLocation?> GetByLocationIdAndDayIndexAsync(Guid tripId, int dayIndex, Guid locationId, CancellationToken cancellationToken)
        {
            return await _dbSet
               .Include(l => l.TripDay)
               .FirstOrDefaultAsync(l =>
                   l.TripDay.TripId == tripId &&
                   l.TripDay.DayIndex == dayIndex &&
                   l.LocationId == locationId,
                   cancellationToken);
        }
    }
}