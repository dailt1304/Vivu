using System.Linq;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Domain.AI;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;

namespace Vivu.Infrastructure.Repository
{
    public class TripRepository : GenericRepository<Trip>, ITripRepository
    {
        public TripRepository(VivuDbContext context) : base(context)
        {
        }

        public override async Task<Trip?> GetByIdAsync(Guid id)
        {
            return await _dbSet
                .Where(t => !t.IsDeleted)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public IQueryable<Trip> GetTripsByUserIdQuery(Guid userId)
        {
            return _dbSet.AsNoTracking()
                .Where(t => !t.IsDeleted && 
                    (t.UserId == userId || t.TripMembers.Any(m => m.UserId == userId)))
                .Include(t => t.TripRating)
                .Include(t => t.City);
        }

        public IQueryable<Trip> GetPublicTripsQuery()
        {
            return _dbSet.AsNoTracking()
                .Where(t => !t.IsDeleted && t.IsPublic)
                .Include(t => t.User)
                    .ThenInclude(u => u.UserProfile)
                .Include(t => t.City)
                    .ThenInclude(c => c.Country)
                .Include(t => t.TripMembers)
                .Include(t => t.TripFavorites);
        }

        public async Task<Trip?> GetTripByIdWithDetailsAsync(Guid tripId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(t => !t.IsDeleted)
                .Include(t => t.User)
                    .ThenInclude(u => u.UserProfile)
                .Include(t => t.TripMembers)
                    .ThenInclude(m => m.User)
                        .ThenInclude(u => u.UserProfile)
                .Include(t => t.TripDays.OrderBy(d => d.DayIndex))
                    .ThenInclude(d => d.TripLocations.OrderBy(l => l.OrderIndex))
                        .ThenInclude(l => l.Location)
                            .ThenInclude(loc => loc.Category)
                .Include(t => t.TripDays.OrderBy(d => d.DayIndex))
                    .ThenInclude(d => d.TripLocations.OrderBy(l => l.OrderIndex))
                        .ThenInclude(l => l.Location)
                            .ThenInclude(loc => loc.LocationDetail)
                .Include(t => t.TripDays.OrderBy(d => d.DayIndex))
                    .ThenInclude(d => d.TripLocations.OrderBy(l => l.OrderIndex))
                        .ThenInclude(l => l.Alternatives.OrderBy(a => a.Priority))
                            .ThenInclude(a => a.Location)
                                .ThenInclude(loc => loc.Category)
                .Include(t => t.TripDays.OrderBy(d => d.DayIndex))
                    .ThenInclude(d => d.TripLocations.OrderBy(l => l.OrderIndex))
                        .ThenInclude(l => l.Alternatives.OrderBy(a => a.Priority))
                            .ThenInclude(a => a.Location)
                                .ThenInclude(loc => loc.LocationDetail)
                .FirstOrDefaultAsync(t => t.Id == tripId, cancellationToken);
        }

        public async Task<Trip?> GetTripByIdWithDetailsForAIModifyAsync(Guid tripId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(t => !t.IsDeleted)
                .Include(t => t.User)
                    .ThenInclude(u => u.UserProfile)
                .Include(t => t.TripMembers)
                    .ThenInclude(m => m.User)
                        .ThenInclude(u => u.UserProfile)
                .Include(t => t.TripDays.OrderBy(d => d.DayIndex))
                    .ThenInclude(d => d.TripLocations.OrderBy(l => l.OrderIndex))
                        .ThenInclude(l => l.Location)
                            .ThenInclude(loc => loc.LocationDetail)
                .Include(t => t.TripDays.OrderBy(d => d.DayIndex))
                    .ThenInclude(d => d.TripLocations.OrderBy(l => l.OrderIndex))
                        .ThenInclude(l => l.Alternatives.OrderBy(a => a.Priority))
                            .ThenInclude(a => a.Location)
                .FirstOrDefaultAsync(t => t.Id == tripId, cancellationToken);
        }

        public async Task<Trip?> GetTripWithRatingAsync(Guid tripId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(t => !t.IsDeleted)
                .Include(t => t.TripRating)
                .FirstOrDefaultAsync(t => t.Id == tripId, cancellationToken);
        }
        public async Task<Trip?> GetTripWithMembersAsync(Guid tripId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(t => !t.IsDeleted)
                .Include(t => t.TripMembers)
                .FirstOrDefaultAsync(t => t.Id == tripId, cancellationToken);
        }

        public async Task<List<Trip>> GetTripsForStatusUpdateAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(t => !t.IsDeleted && (t.Status == "planning" || t.Status == "ongoing"))
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> CheckAvailableInviteCode(string code, CancellationToken cancellationToken)
        {
            return !await _context.Trips
                    .AnyAsync(t => t.InviteCode == code && !t.IsDeleted, cancellationToken);
        }

        public async Task<Trip?> GetByInviteCodeAsync(string InviteCode, CancellationToken cancellationToken)
        {
            return await _dbSet
            .Where(t => t.InviteCode == InviteCode && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
        }

        public IQueryable<Trip> GetSearchTermTrips(string tsQueryString)
        {
            return _dbSet
                .AsNoTracking()
                .Where(t => !t.IsDeleted && t.IsPublic)
                .Include(t => t.User)
                    .ThenInclude(u => u.UserProfile)
                .Include(t => t.City)
                    .ThenInclude(c => c.Country)
                .Include(t => t.TripMembers)
                .Include(t => t.TripFavorites)
                .Where(t => EF.Property<NpgsqlTsVector>(t, "SearchVector")
                    .Matches(EF.Functions.ToTsQuery("simple", tsQueryString)));
        }

        public async Task<List<TripHistorySummary>> GetUserTripHistoryAsync(Guid userId, CancellationToken ct = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(t => !t.IsDeleted && t.UserId == userId)
                .OrderByDescending(t => t.StartDate)
                .Select(t => new TripHistorySummary(
                    t.CityId ?? Guid.Empty,
                    t.City != null ? t.City.Name : string.Empty,
                    t.TripDays
                        .Where(d => d.DayIndex > 0)
                        .SelectMany(d => d.TripLocations.OrderBy(l => l.OrderIndex))
                        .Where(l => l.LocationId != null)
                        .Select(l => l.LocationId)
                        .Distinct()
                        .Take(10) // up to 10 top locations per trip
                        .ToList()
                ))
                .ToListAsync(ct);
        }

        // Statistics
        public async Task<int> GetTotalCountAsync(CancellationToken ct = default)
            => await _dbSet.CountAsync(t => !t.IsDeleted, ct);
    }
}
