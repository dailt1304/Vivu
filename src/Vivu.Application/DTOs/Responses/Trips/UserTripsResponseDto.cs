using Vivu.Application.Common.Models;

namespace Vivu.Application.DTOs.Responses.Trips
{
    public class UserTripsResponseDto
    {
        public PaginatedList<TripDto> Trips { get; set; } = null!;
        public int NumberOfTripCreated { get; set; }
        public int TripLimit { get; set; }
    }
}
