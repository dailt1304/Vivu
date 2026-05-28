using Vivu.Application.DTOs.Responses.AI;
using Vivu.Domain.Entities;
using Vivu.Domain.Shared;

namespace Vivu.Application.Interfaces.AI
{
    public interface IAIParsing
    {
        Task<Result<TripPlanResponse>> ParseTripPlanResponse(string jsonContent, List<Location> availableLocations);
        Task<Result<TripModificationResponse>> ParseModifyTripResponse(string jsonContent, Dictionary<int, Guid> locationMap);
        Task<TripConstraints> ParseNotesConstraintsAsync(
                string notes,
                List<string> availableCategories,
                CancellationToken cancellationToken = default);
    }
}
