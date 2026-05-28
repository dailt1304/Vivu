using Vivu.Domain.Entities;

namespace Vivu.Application.Interfaces.Trips
{
    public interface IPublicTripService
    {
        IQueryable<Trip> ApplyDurationFilter(IQueryable<Trip> query, int duration);
        IQueryable<Trip> ApplySorting(IQueryable<Trip> query, string sortBy);
        int? CalculateDurationDays(DateTime? startDate, DateTime? endDate);
        double? CalculateTrendingScore(int favoritesCount, int views, DateTime createdDate);
    }
}
