using Vivu.Application.DTOs.Responses.AI;
using Vivu.Domain.Enums;
using LocationEntity = Vivu.Domain.Entities.Location;

namespace Vivu.Application.Interfaces.AI
{
    public interface ILocationZoneService
    {
        List<LocationEntity> SelectLocationsForTrip(
            List<LocationEntity> allLocations,
            int daysCount,
            List<string> preferences,
            TripConstraints? constraints = null,
            UserPersonalizationContext? personalization = null);

        List<LocationEntity> SelectLocationsForModify(
            List<LocationEntity> allLocations,
            List<LocationEntity> currentTripLocations,
            string userRequest,
            UserPersonalizationContext? personalization = null,
            TripConstraints? constraints = null);
    }
}
