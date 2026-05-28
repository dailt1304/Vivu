using Vivu.Application.DTOs.Responses.AI;

namespace Vivu.Application.Interfaces.AI
{
    /// <summary>
    /// Abstraction for tracking background AI trip generation progress.
    /// </summary>
    public interface IGenerationTrackingService
    {
        /// <summary>
        /// Mark a generation as in-progress. Called at the start of StreamGenerateTrip.
        /// </summary>
        /// <param name="generationId">Client-generated unique tracking ID</param>
        /// <param name="userId">The user who initiated the generation</param>
        void SetProcessing(string generationId, string userId);

        /// <summary>
        /// Mark a generation as successfully completed with the new trip ID.
        /// Called when StreamEventType.Saved is received.
        /// </summary>
        void SetCompleted(string generationId, string userId, string tripId);

        /// <summary>
        /// Mark a generation as failed with an error message.
        /// Called when the stream encounters an error or failure.
        /// </summary>
        void SetFailed(string generationId, string userId, string error);

        /// <summary>
        /// Retrieve the current status of a generation.
        /// Returns null if the generation ID is unknown/expired or the caller is not the owner.
        /// </summary>
        GenerationStatusDto? GetStatus(string generationId, string userId);

        /// <summary>
        /// Retrieve the current status of a generation (anonymous access).
        /// The generationId (UUID) itself serves as the authentication secret.
        /// Returns null if the generation ID is unknown/expired.
        /// </summary>
        GenerationStatusDto? GetStatus(string generationId);
    }
}
