namespace Vivu.Application.Interfaces.Trips
{
    public interface ISearchTermTrip
    {
        string SanitizeSearchTerm(string searchTerm);
        string ConvertToTsQuery(string sanitizedTerm);
    }
}
