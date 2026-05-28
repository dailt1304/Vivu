namespace Vivu.Domain.AI
{

    public record TripHistorySummary(
        Guid CityId,
        string CityName,
        List<Guid> VisitedLocationNames
    );
}
