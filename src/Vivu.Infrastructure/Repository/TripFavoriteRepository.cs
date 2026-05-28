using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vivu.Application.Interfaces.TripFavourites;
using Vivu.Application.UseCases.Trips.Queries.GetUserFavoriteTrips;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;

namespace Vivu.Infrastructure.Repository
{
    public class TripFavoriteRepository : GenericRepository<TripFavorite>, ITripFavoriteRepository, IFilterTripFavourite
    {
        public TripFavoriteRepository(VivuDbContext context) : base(context)
        {
        }

        public IQueryable<TripFavorite> ApplySorting(IQueryable<TripFavorite> query, GetUserFavoriteTripsQuery request)
        {
            if (string.IsNullOrWhiteSpace(request.SortColumn))
            {
                return request.SortDescending
                    ? query.OrderByDescending(x => x.CreatedAt)
                    : query.OrderBy(x => x.CreatedAt);
            }

            return request.SortColumn.ToLower() switch
            {
                "title" => request.SortDescending
                    ? query.OrderByDescending(x => x.Trip.Title)
                    : query.OrderBy(x => x.Trip.Title),

                "startdate" => request.SortDescending
                    ? query.OrderByDescending(x => x.Trip.StartDate)
                    : query.OrderBy(x => x.Trip.StartDate),

                "createdat" => request.SortDescending
                    ? query.OrderByDescending(x => x.Trip.CreatedDate)
                    : query.OrderBy(x => x.Trip.CreatedDate),

                "favoritedat" => request.SortDescending
                    ? query.OrderByDescending(x => x.CreatedAt)
                    : query.OrderBy(x => x.CreatedAt),

                _ => request.SortDescending
                    ? query.OrderByDescending(x => x.CreatedAt)
                    : query.OrderBy(x => x.CreatedAt)
            };
        }

        public async Task<TripFavorite?> GetTripFavouriteById(Guid userId, Guid tripId, CancellationToken cancellationToken)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    tf => tf.TripId ==tripId && tf.UserId == userId,
                    cancellationToken);
        }

        public IQueryable<TripFavorite> GetUserTripFavourite(Guid userId)
        {
            return _dbSet.AsNoTracking()
                .Where(tf => tf.UserId == userId)
                .Include(tf => tf.Trip)
                    .ThenInclude(t => t.City)
                .Include(tf => tf.Trip)
                    .ThenInclude(t => t.User)
                        .ThenInclude(u => u.UserProfile)
                .Include(tf => tf.Trip)
                    .ThenInclude(t => t.TripMembers)
                .Where(tf => !tf.Trip.IsDeleted && tf.Trip.IsPublic);
        }
    }
}
