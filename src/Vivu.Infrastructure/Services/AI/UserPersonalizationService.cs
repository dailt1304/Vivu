using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.Interfaces.AI;
using Vivu.Domain.Enums;
using Vivu.Domain.Interfaces;

namespace Vivu.Infrastructure.Services.AI
{
    public class UserPersonalizationService : IUserPersonalizationService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITripRepository _tripRepository;

        public UserPersonalizationService(IUserRepository userRepository, ITripRepository tripRepository)
        {
            _userRepository = userRepository;
            _tripRepository = tripRepository;
        }

        public async Task<UserPersonalizationContext> BuildContextAsync(
            Guid userId,
            int groupSize,
            GroupCompositionType groupComposition,
            CancellationToken ct = default)
        {
            var context = new UserPersonalizationContext
            {
                GroupComposition = groupComposition,
                PersonalityEntropySeed = Math.Abs(userId.GetHashCode()) % 1000
            };

            // 1. Fetch user Profile (for demographics)
            var user = await _userRepository.GetByIdAsync(userId);
            if (user?.UserProfile != null)
            {
                context.Gender = user.UserProfile.Gender?.ToLowerInvariant();
                
                if (user.UserProfile.DateOfBirth.HasValue)
                {
                    var age = DateTime.UtcNow.Year - user.UserProfile.DateOfBirth.Value.Year;
                    if (user.UserProfile.DateOfBirth.Value.Date > DateTime.UtcNow.AddYears(-age)) age--;

                    context.AgeGroup = age switch
                    {
                        < 13 => "child",
                        < 18 => "teenager",
                        < 35 => "young_adult",
                        < 55 => "adult",
                        _ => "senior"
                    };
                }
            }

            // 2. Fetch Trip History
            context.TripHistory = await _tripRepository.GetUserTripHistoryAsync(userId, ct);

            // Flatten all visited locations to easily pass to exclusion logic
            context.AllVisitedLocationIds = context.TripHistory
                .SelectMany(t => t.VisitedLocationNames)
                .Distinct()
                .ToList();

            // 3. Infer Travel Style
            context.TravelStyle = InferTravelStyle(groupComposition, context.AgeGroup, context.Gender, groupSize);

            return context;
        }

        private static string InferTravelStyle(
            GroupCompositionType groupComposition, 
            string? ageGroup, 
            string? gender, 
            int groupSize)
        {
            if (groupComposition == GroupCompositionType.FamilyWithChildren)
                return "family";

            if (groupComposition == GroupCompositionType.FriendsGroup)
            {
                if (ageGroup == "young_adult" && gender == "male")
                    return "young_group"; // specific vibe: e.g. nightlife, spontaneous
                return "friends";
            }

            if (groupComposition == GroupCompositionType.Couple)
                return "couple";

            if (groupComposition == GroupCompositionType.Solo)
                return "solo";

            if (groupComposition == GroupCompositionType.Senior || ageGroup == "senior")
                return "senior";

            // Fallbacks based solely on group size if composition enum was left to 'General'
            if (groupSize >= 4 && ageGroup == "young_adult")
                return "young_group";
            
            if (groupSize == 2)
                return "couple";
                
            if (groupSize == 1)
                return "solo";

            return "general";
        }
    }
}
