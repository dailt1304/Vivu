using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Domain.Entities;

namespace Vivu.Domain.Interfaces
{
    public interface ITripFavoriteRepository : IGenericRepository<TripFavorite>
    {
        Task<TripFavorite?> GetTripFavouriteById(Guid userId, Guid tripId, CancellationToken cancellationToken);
        IQueryable<TripFavorite> GetUserTripFavourite(Guid userId);
    }
}
