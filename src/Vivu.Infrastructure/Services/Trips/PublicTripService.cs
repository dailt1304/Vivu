using Microsoft.EntityFrameworkCore;
using Vivu.Application.Interfaces.Trips;
using Vivu.Domain.Entities;

namespace Vivu.Infrastructure.Services.Trips
{
    public class PublicTripService : IPublicTripService
    {
        public IQueryable<Trip> ApplyDurationFilter(IQueryable<Trip> query, int duration)
        {
            // Duration logic:
            // - 1 day trip: start date = end date (days difference = 0)
            // - 2 day trip: days difference = 1
            // - N day trip: days difference = N - 1
            var daysDifference = duration - 1;

            return query.Where(t => 
                t.StartDate != null && 
                t.EndDate != null &&
                (t.EndDate.Value.Date - t.StartDate.Value.Date).Days == daysDifference);
        }

        public IQueryable<Trip> ApplySorting(IQueryable<Trip> query, string sortBy)
        {
            return sortBy.ToLower() switch
            {
                "newest" => query.OrderByDescending(t => t.CreatedDate),

                "popular" => query.OrderByDescending(t => t.TripFavorites.Count),

                "trending" => query.OrderByDescending(t =>
                    (double)t.TripFavorites.Count / ((DateTime.UtcNow.Date - t.CreatedDate.Date).Days + 1)),

                _ => query.OrderByDescending(t => t.CreatedDate)
            };
        }

        public int? CalculateDurationDays(DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue || !endDate.HasValue)
                return null;

            return (endDate.Value - startDate.Value).Days + 1;
        }

        public double? CalculateTrendingScore(int favoritesCount, int views, DateTime createdDate)
        {
            var daysSinceCreated = (DateTime.UtcNow - createdDate).Days + 1;
            return (favoritesCount + views) / (double)daysSinceCreated;
        }
    }
}
