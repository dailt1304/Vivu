using Vivu.Application.DTOs.Responses.AI;
using LocationEntity = Vivu.Domain.Entities.Location;

namespace Vivu.Application.Interfaces.AI
{
    public interface ITripItineraryOptimizer
    {
        /// <summary>
        /// Reorders flexible locations (Attraction/Shopping/Entertainment) within each day
        /// using Nearest Neighbor heuristic, while keeping Anchor locations (Food/Accommodation)
        /// fixed in their original time slots.
        /// </summary>
        TripPlanResponse Optimize(TripPlanResponse tripPlan, List<LocationEntity> selectedLocations);
    }
}
