namespace Vivu.Application.DTOs.Responses.AI
{
    public class AlternativeLocationDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("locationId")]
        public Guid LocationId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("priority")]
        public int Priority { get; set; } = 1;
    }
}
