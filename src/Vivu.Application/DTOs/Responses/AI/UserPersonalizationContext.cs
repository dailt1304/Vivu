using Vivu.Domain.AI;
using Vivu.Domain.Enums;

namespace Vivu.Application.DTOs.Responses.AI
{
    public class UserPersonalizationContext
    {
        // ── Demographics ───────────────────────────────────────────────────────
        ///"child" | "teenager" | "young_adult" | "adult" | "senior" | null
        public string? AgeGroup { get; set; }

        /// "male" | "female" | null (unknown/mixed
        public string? Gender { get; set; }

        public GroupCompositionType GroupComposition { get; set; } = GroupCompositionType.General;

        public List<TripHistorySummary> TripHistory { get; set; } = new();


        public List<Guid> AllVisitedLocationIds { get; set; } = new();


        /// "family" | "young_group" | "friends" | "couple" | "solo" | "senior" | "general"
        public string TravelStyle { get; set; } = "general";

        public int PersonalityEntropySeed { get; set; }
    }
}
