using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.Interfaces.AI;
using Vivu.Domain.AI;
using Vivu.Domain.Entities;
using Vivu.Domain.Shared;
using Vivu.Infrastructure.Services.AI.Converters;
using Vivu.Infrastructure.Services.AI.Factories;
using static Vivu.Domain.Errors.DomainErrors;

namespace Vivu.Infrastructure.Services.AI
{
    public class AIParsing : IAIParsing
    {
        private readonly ILogger<AIParsing> _logger;
        private readonly IPromptBuilder _promptBuilder;
        private readonly IAIClientFactory _clientFactory;

        public AIParsing(ILogger<AIParsing> logger, IPromptBuilder promptBuilder, IAIClientFactory clientFactory)
        {
            _promptBuilder = promptBuilder;
            _logger = logger;
            _clientFactory = clientFactory;
        }
        public async Task<Result<TripPlanResponse>> ParseTripPlanResponse(string jsonContent, List<Domain.Entities.Location> availableLocations)
        {
            try
            {
                var cleanJson = jsonContent
                    .Trim()
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                cleanJson = ResolveLocationIndexes(cleanJson, availableLocations);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters =
                    {
                        new DateOnlyJsonConverter(),
                        new TimeOnlyJsonConverter()
                    }
                };
                Console.WriteLine("Clean json : " + cleanJson);
                var result = JsonSerializer.Deserialize<TripPlanResponse>(cleanJson, options);

                if (result == null)
                {
                    return Result<TripPlanResponse>.Failure(AIErrors.ParseError("Deserialization returned null"));
                }

                if (string.IsNullOrEmpty(result.Title))
                {
                    return Result<TripPlanResponse>.Failure(AIErrors.ParseError("Missing trip title"));
                }

                if (!result.Days.Any())
                {
                    return Result<TripPlanResponse>.Failure(AIErrors.ParseError("No days in itinerary"));
                }

                foreach (var day in result.Days)
                {
                    if (!day.Locations.Any())
                    {
                        return Result<TripPlanResponse>.Failure(AIErrors.ParseError($"Day {day.DayIndex} has no locations"));
                    }
                }

                _logger.LogDebug("Successfully parsed AI response: {Days} days, {Locations} locations",
                    result.Days.Count, result.Days.Sum(d => d.Locations.Count));

                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing failed. Content: {Content}", jsonContent);
                return Result<TripPlanResponse>.Failure(AIErrors.ParseError($"Invalid JSON format: {ex.Message}"));
            }
        }
        private string ResolveLocationIndexes(string json, List<Domain.Entities.Location> locations)
        {
            return Regex.Replace(json, @"""locationIndex""\s*:\s*(\d+)", match =>
            {
                if (int.TryParse(match.Groups[1].Value, out var index) && index >= 1 && index <= locations.Count)
                    return $"\"locationId\": \"{locations[index - 1].Id}\"";

                _logger.LogWarning("locationIndex {Index} out of range (available: {Count})", match.Groups[1].Value, locations.Count);
                return match.Value;
            });
        }

        public async Task<Result<TripModificationResponse>> ParseModifyTripResponse(string jsonContent, Dictionary<int, Guid> locationMap)
        {
            try
            {
                var cleanJson = jsonContent
                    .Trim()
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                cleanJson = ResolveModificationIndexes(cleanJson, locationMap);
                _logger.LogDebug("Clean json Modify Trip before map: {cleanjson}", cleanJson);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters =
                    {
                        new DateOnlyJsonConverter(),
                        new TimeOnlyJsonConverter()
                    }
                };

                var result = JsonSerializer.Deserialize<TripModificationResponse>(cleanJson, options);
                var logOptions = new JsonSerializerOptions { WriteIndented = true };
                var jsonString = JsonSerializer.Serialize(result, logOptions);
                _logger.LogDebug("Object Modify Trip after map:\n{TripModificationData}", jsonString);


                if (result == null)
                    return Result<TripModificationResponse>.Failure(AIErrors.ParseError("Deserialization returned null"));

                if (!result.Changes.Any())
                    return Result<TripModificationResponse>.Failure(AIErrors.ParseError("No changes found in modification response"));

                _logger.LogDebug("Successfully parsed modification response: {Count} changes", result.Changes.Count);

                return Result<TripModificationResponse>.Success(result);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing failed for modify response. Content: {Content}", jsonContent);
                return Result<TripModificationResponse>.Failure(AIErrors.ParseError($"Invalid JSON format: {ex.Message}"));
            }
        }

        private string ResolveModificationIndexes(string json, Dictionary<int, Guid> map)
        {
            var replaced = Regex.Replace(json, @"""locationIndex""\s*:\s*(\d+)", match =>
            {
                if (int.TryParse(match.Groups[1].Value, out var index) && map.TryGetValue(index, out var id))
                    return $"\"locationId\": \"{id}\"";
                return match.Value;
            });

            return Regex.Replace(replaced, @"""oldLocationIndex""\s*:\s*(\d+)", match =>
            {
                if (int.TryParse(match.Groups[1].Value, out var index) && map.TryGetValue(index, out var id))
                    return $"\"oldLocationId\": \"{id}\"";
                return match.Value;
            });
        }

        public async Task<TripConstraints> ParseNotesConstraintsAsync(
                string notes,
                List<string> availableCategories,
                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(notes))
                return new TripConstraints();

            var prompt = _promptBuilder.BuildNotesConstraintsPrompt(notes, availableCategories);

            try
            {
                var client = _clientFactory.GetClient();
                var result = await client.SendRequestAsync(
                    prompt,
                    new AIRequestOptions { Temperature = 0.1 },
                    cancellationToken);

                if (result.IsFailure)
                    return new TripConstraints(); 

                var cleanJson = result.Value!.Content.Trim()
                    .Replace("```json", "").Replace("```", "").Trim();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new TimeOnlyJsonConverter() }
                };

                return JsonSerializer.Deserialize<TripConstraints>(cleanJson, options)
                       ?? new TripConstraints();
            }
            catch
            {
                return new TripConstraints(); 
            }
        }
    }
}
