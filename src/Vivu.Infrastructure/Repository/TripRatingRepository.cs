using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;

namespace Vivu.Infrastructure.Repository
{
    public class TripRatingRepository : GenericRepository<TripRating>, ITripRatingRepository
    {
        public TripRatingRepository(VivuDbContext context) : base(context)
        {
        }
    }
}
