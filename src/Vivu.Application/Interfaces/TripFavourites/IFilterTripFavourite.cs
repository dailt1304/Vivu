using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.UseCases.Trips.Queries.GetUserFavoriteTrips;
using Vivu.Domain.Entities;

namespace Vivu.Application.Interfaces.TripFavourites
{
    public interface IFilterTripFavourite
    {
       IQueryable<TripFavorite> ApplySorting(
            IQueryable<TripFavorite> query,
            GetUserFavoriteTripsQuery request);
    }
}
