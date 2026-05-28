namespace Vivu.Application.Interfaces.Trips
{
    /// <summary>
    /// Abstraction for broadcasting trip member changes via SignalR.
    /// Implementation lives in WebApi layer (TripHubService).
    /// </summary>
    public interface ITripHubService
    {
        /// <summary>
        /// Broadcast member list changed event to all members in the trip group.
        /// </summary>
        /// <param name="tripId">The trip ID</param>
        /// <param name="action">"joined" | "left" | "removed"</param>
        /// <param name="changedUserId">The user who joined/left/was removed</param>
        /// <param name="changedUserName">Display name of the changed user</param>
        Task BroadcastMemberChangedAsync(
            Guid tripId, string action, Guid changedUserId, string? changedUserName);

        /// <summary>
        /// Notify a specific user that they have been kicked from a trip.
        /// Sends via trip SignalR group; frontend filters by KickedUserId.
        /// </summary>
        /// <param name="tripId">The trip ID</param>
        /// <param name="kickedUserId">The user being kicked</param>
        /// <param name="tripTitle">Title of the trip for UX messaging</param>
        Task NotifyMemberKickedAsync(
            Guid tripId, Guid kickedUserId, string tripTitle);

        /// <summary>
        /// Broadcast itinerary changed event (location added/updated/removed)
        /// to all members in the trip group so they can refresh their view.
        /// </summary>
        /// <param name="tripId">The trip ID</param>
        /// <param name="action">"location_added" | "location_updated" | "location_removed"</param>
        /// <param name="changedByUserId">The user who made the change</param>
        Task BroadcastItineraryChangedAsync(
            Guid tripId, string action, Guid changedByUserId);
    }
}
