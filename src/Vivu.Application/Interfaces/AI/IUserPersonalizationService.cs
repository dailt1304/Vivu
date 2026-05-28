using Vivu.Application.DTOs.Responses.AI;
using Vivu.Domain.Enums;

namespace Vivu.Application.Interfaces.AI
{
    public interface IUserPersonalizationService
    {
        /// <summary>
        /// Builds a complete personalization context for the given user.
        /// Loads UserProfile (age/gender) + full trip history,
        /// infers travel style, and computes a per-user entropy seed.
        /// </summary>
        Task<UserPersonalizationContext> BuildContextAsync(
            Guid userId,
            int groupSize,
            GroupCompositionType groupComposition,
            CancellationToken ct = default);
    }
}
