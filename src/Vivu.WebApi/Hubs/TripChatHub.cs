using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Polly;
using System.Text.RegularExpressions;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.ChatMessages.Commands.SendChatMessage;
using Vivu.Domain.Interfaces;

namespace Vivu.WebApi.Hubs
{
    [Authorize]
    public class TripChatHub : Hub
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<TripChatHub> _logger;
        private readonly ITripMemberRepository _tripMemberRepository;

        public TripChatHub(IMediator mediator, ICurrentUser currentUser, ILogger<TripChatHub> logger, ITripMemberRepository tripMemberRepository)
        {
            _mediator = mediator;
            _tripMemberRepository = tripMemberRepository;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task JoinTrip(string tripId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"trip-{tripId}");
            _logger.LogDebug("User {UserId} joined trip room {TripId}", _currentUser.Id, tripId);
        }

        public async Task LeaveTrip(string tripId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"trip-{tripId}");
            _logger.LogDebug("User {UserId} left trip room {TripId}", _currentUser.Id, tripId);
        }
        public override async Task OnConnectedAsync()
        {
            if (Guid.TryParse(_currentUser.Id, out var userId))
            {
                var userTripIds = await _tripMemberRepository.GetUserTripIdsAsync(userId);
                foreach (var tripId in userTripIds)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"trip-{tripId}");
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

                _logger.LogInformation("User {UserId} connected and auto-joined {Count} trip groups + personal group.", userId, userTripIds.Count);
            }

            await base.OnConnectedAsync();
        }
        public async Task SendMessage(SendChatMessageCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsFailure)
            {
                await Clients.Caller.SendAsync("Error", result.Error.Message);
                return;
            }

            await Clients.Group($"trip-{command.TripId}")
                .SendAsync("ReceiveMessage", result.Value);
        }

        public async Task BroadcastTripEdit(System.Text.Json.JsonElement payload)
        {
            if (payload.TryGetProperty("tripId", out var tripIdElement))
            {
                var tripId = tripIdElement.GetString();
                if (!string.IsNullOrEmpty(tripId))
                {
                    // Relay the trip edit payload to everyone in the group except the caller (the caller already updated their UI locally)
                    await Clients.OthersInGroup($"trip-{tripId}").SendAsync("TripUpdated", payload);
                }
            }
        }


        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogDebug("User {UserId} disconnected", _currentUser.Id);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
