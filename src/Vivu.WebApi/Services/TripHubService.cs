using Microsoft.AspNetCore.SignalR;
using Vivu.Application.Interfaces.Trips;
using Vivu.WebApi.Hubs;

namespace Vivu.WebApi.Services
{
    /// <summary>
    /// Implementation of ITripHubService using IHubContext&lt;TripChatHub&gt;.
    /// Broadcasts trip member changes to SignalR groups.
    /// </summary>
    public class TripHubService : ITripHubService
    {
        private readonly IHubContext<TripChatHub> _hubContext;

        public TripHubService(IHubContext<TripChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task BroadcastMemberChangedAsync(
            Guid tripId, string action, Guid changedUserId, string? changedUserName)
        {
            await _hubContext.Clients.Group($"trip-{tripId}")
                .SendAsync("MemberListChanged", new
                {
                    TripId = tripId,
                    Action = action,
                    ChangedUserId = changedUserId,
                    ChangedUserName = changedUserName ?? "Người dùng",
                    Timestamp = DateTime.UtcNow
                });
        }

        public async Task NotifyMemberKickedAsync(
            Guid tripId, Guid kickedUserId, string tripTitle)
        {
            // Send to trip group — the kicked user is still connected until they disconnect.
            // Frontend filters by KickedUserId to only react for the correct user.
            await _hubContext.Clients.Group($"trip-{tripId}")
                .SendAsync("MemberKicked", new
                {
                    TripId = tripId,
                    KickedUserId = kickedUserId,
                    TripTitle = tripTitle,
                    Timestamp = DateTime.UtcNow
                });
        }

        public async Task BroadcastItineraryChangedAsync(
            Guid tripId, string action, Guid changedByUserId)
        {
            await _hubContext.Clients.Group($"trip-{tripId}")
                .SendAsync("TripUpdated", new
                {
                    tripId = tripId,
                    type = action,
                    changedBy = changedByUserId,
                    timestamp = DateTime.UtcNow
                });
        }
    }
}
