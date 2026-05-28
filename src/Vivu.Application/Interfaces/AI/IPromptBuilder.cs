using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.UseCases.AI.StreamGenerateTrip;
using Vivu.Domain.Entities;

namespace Vivu.Application.Interfaces.AI
{
    public interface IPromptBuilder
    {
        string BuildTripPlanPrompt(TripPlanDto request, List<Location> availableLocations, TripConstraints? constraints = null, UserPersonalizationContext? personalization = null);
        string BuildUserPromptFromForm(StreamGenerateTripCommand request, string cityName);
        string BuildModifyTripPrompt(TripPlanResponse currentTrip, List<ChatMessage> recentMessages,
                                    List<Location> availableLocations, string userRequest,
                                    UserPersonalizationContext? personalization = null,
                                    TripConstraints? constraints = null);
        string BuildDetectIntentPrompt(string userMessage);
        string BuildChatMessagePrompt(Trip trip, List<ChatMessage> recentMessages, string userMessage);
        string BuildNotesConstraintsPrompt(string notes, List<string> availableCategories);
    }
}
