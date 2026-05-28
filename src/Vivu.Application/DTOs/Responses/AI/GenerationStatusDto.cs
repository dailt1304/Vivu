namespace Vivu.Application.DTOs.Responses.AI
{
    /// <summary>
    /// Tracks the progress of a background AI trip generation.
    /// Stored in IMemoryCache with a 10-minute sliding expiry.
    /// </summary>
    public class GenerationStatusDto
    {
        /// <summary>
        /// Current status: "processing" | "completed" | "failed"
        /// </summary>
        public string Status { get; set; } = "processing";

        /// <summary>
        /// The ID of the successfully created trip. Set when Status = "completed".
        /// </summary>
        public string? TripId { get; set; }

        /// <summary>
        /// Error message when Status = "failed".
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// The user who initiated this generation. Used for ownership verification.
        /// </summary>
        public string? UserId { get; set; }
    }
}
