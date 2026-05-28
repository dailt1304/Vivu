using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.AI;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.AI.CreateTrip;
using Vivu.Application.UseCases.AI.StreamAIChat;
using Vivu.Application.UseCases.AI.StreamGenerateTrip;
using Vivu.Application.UseCases.AI.StreamModifyTrip;
using Vivu.Domain.Enums;
using Vivu.WebApi.Hubs;

namespace Vivu.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AIController : ApiControllerBase
    {
        private readonly ILogger<AIController> _logger;
        private readonly IHubContext<TripChatHub> _hubContext;
        private readonly IGenerationTrackingService _generationTracking;
        private readonly ICurrentUser _currentUser;

        public AIController(
            ILogger<AIController> logger,
            IHubContext<TripChatHub> hubContext,
            IGenerationTrackingService generationTracking,
            ICurrentUser currentUser)
        {
            _logger = logger;
            _hubContext = hubContext;
            _generationTracking = generationTracking;
            _currentUser = currentUser;
        }

        [HttpPost("ai-create-trip")]
        public async Task<IActionResult> AICreateTrip([FromBody] AICreateTripCommand command)
        {
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("trips/{tripId}/chat")]
        public async Task StreamAIChat(
            Guid tripId,
            [FromBody] StreamAIDetectIntentCommand command,
            [FromQuery] string? connectionId,
            CancellationToken cancellationToken)
        {
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            try
            {
                command.TripId = tripId;
                object? completeData = null;
                object? savedData = null;

                await foreach (var chunk in Mediator.CreateStream(command, CancellationToken.None))
                {
                    if (chunk.IsFailure)
                    {
                        //Guard each SSE write; the HTTP connection may have ended.
                        try { await WriteSSEAsync("error", chunk.Error!.Message); } catch { }
                        await BroadcastAIEventToGroupAsync(tripId, "AIError", new
                        {
                            tripId,
                            message = "Đã có lỗi xảy ra. Vui lòng thử lại."
                        });
                        break;
                    }

                    switch (chunk.Value!.Type)
                    {
                        case StreamEventType.Start:
                            try { await WriteSSEAsync("start", JsonSerializer.Serialize(chunk.Value.Data)); } catch { }
                            // BroadcastAIEventToGroupAsync sends to ALL members,
                            // including the caller, so they can receive events after reconnect.
                            await BroadcastAIEventToGroupAsync(tripId, "AIProcessing", new
                            {
                                tripId,
                                status = chunk.Value.Data
                            });
                            break;
                        case StreamEventType.Parsing:
                            try { await WriteSSEAsync("parsing", JsonSerializer.Serialize(chunk.Value.Data)); } catch { }
                            await BroadcastAIEventToGroupAsync(tripId, "AIProcessing", new
                            {
                                tripId,
                                status = chunk.Value.Data
                            });
                            break;
                        case StreamEventType.Chunk:
                            var chunkStr = chunk.Value.Data?.ToString() ?? "";
                            try { await WriteSSEAsync("chunk", chunkStr); } catch { }
                            await BroadcastAIEventToGroupAsync(tripId, "ReceiveAIChunk", chunkStr);
                            break;
                        case StreamEventType.Complete:
                            completeData = chunk.Value.Data;
                            try { await WriteSSEAsync("complete", JsonSerializer.Serialize(chunk.Value.Data)); } catch { }
                            await BroadcastAIEventToGroupAsync(tripId, "ReceiveAIMessage", new
                            {
                                tripId,
                                isAiMessage = true,
                                data = chunk.Value.Data,
                                type = "ai_response"
                            });
                            break;
                        case StreamEventType.Saving:
                            try { await WriteSSEAsync("saving", JsonSerializer.Serialize(chunk.Value.Data)); } catch { }
                            break;
                        case StreamEventType.Saved:
                            savedData = chunk.Value.Data;
                            try { await WriteSSEAsync("saved", JsonSerializer.Serialize(chunk.Value.Data)); } catch { }
                            // TripUpdated still uses GroupExcept to avoid the sender duplicating the UI update
                            await BroadcastToGroupAsync(tripId, connectionId, "TripUpdated", new
                            {
                                tripId,
                                updatedTrip = chunk.Value.Data
                            }, CancellationToken.None);
                            break;
                    }

                    // Flush independently; ignore errors from disconnected clients.
                    try { await Response.Body.FlushAsync(CancellationToken.None); } catch { }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("AI chat stream cancelled by client");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AI chat stream");
                try { await WriteSSEAsync("error", ex.Message); } catch { }
                try
                {
                    await BroadcastAIEventToGroupAsync(tripId, "AIError", new
                    {
                        tripId,
                        message = "Đã có lỗi xảy ra. Vui lòng thử lại."
                    });
                }
                catch { }
            }
        }

        [HttpPost("stream/generate-trip")]
        public async Task StreamGenerateTrip(
            [FromBody] StreamGenerateTripCommand command,
            CancellationToken cancellationToken)
        {
            Guid.TryParse(_currentUser.Id, out var generatingUserId);
            var genId = command.GenerationId;

            // Track generation start in IMemoryCache for polling fallback
            if (!string.IsNullOrEmpty(genId) && generatingUserId != Guid.Empty)
                _generationTracking.SetProcessing(genId, generatingUserId.ToString());

            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            try
            {
                // connection lifetime. If the client reloads, the handler continues,
                // saves the trip, and notifies the frontend via SignalR.
                await foreach (var chunk in Mediator.CreateStream(command, CancellationToken.None))
                {
                    if (chunk.IsFailure)
                    {
                        try { await WriteSSEAsync("error", chunk.Error!.Message); } catch { }
                        if (generatingUserId != Guid.Empty)
                        {
                            // Track failure in cache for polling fallback
                            if (!string.IsNullOrEmpty(genId))
                                _generationTracking.SetFailed(genId, generatingUserId.ToString(), chunk.Error!.Message);

                            try
                            {
                                await _hubContext.Clients
                                    .Group($"user-{generatingUserId}")
                                    .SendAsync("TripGenerationFailed",
                                        new { message = chunk.Error!.Message },
                                        CancellationToken.None);
                            }
                            catch { }
                        }
                        break;
                    }

                    switch (chunk.Value!.Type)
                    {
                        case StreamEventType.Start:
                            try { await WriteSSEAsync("start", JsonSerializer.Serialize(chunk.Value.Data)); } catch { }
                            break;

                        case StreamEventType.LocationMap:
                            try { await WriteSSEAsync("location_map", JsonSerializer.Serialize(chunk.Value.Data)); } catch { }
                            break;

                        case StreamEventType.Parsing:
                            try { await WriteSSEAsync("parsing", JsonSerializer.Serialize(chunk.Value.Data)); } catch { }
                            break;

                        case StreamEventType.Chunk:
                            try { await WriteSSEAsync("chunk", chunk.Value.Data?.ToString() ?? ""); } catch { }
                            break;

                        case StreamEventType.Complete:
                            try { await WriteSSEAsync("complete", JsonSerializer.Serialize(chunk.Value.Data)); } catch { }
                            break;

                        case StreamEventType.Saving:
                            try { await WriteSSEAsync("saving", "Saving trip to database..."); } catch { }
                            break;

                        case StreamEventType.Saved:
                            // group so the "waiting" UI after a reload can navigate without re-generating.
                            try { await WriteSSEAsync("saved", JsonSerializer.Serialize(chunk.Value.Data)); } catch { }
                            if (generatingUserId != Guid.Empty)
                            {
                                // Track completion in cache for polling fallback
                                if (!string.IsNullOrEmpty(genId) && chunk.Value.Data is DetailedTripDto savedTrip)
                                    _generationTracking.SetCompleted(genId, generatingUserId.ToString(), savedTrip.Id.ToString());

                                try
                                {
                                    await _hubContext.Clients
                                        .Group($"user-{generatingUserId}")
                                        .SendAsync("TripGenerationCompleted", chunk.Value.Data, CancellationToken.None);
                                }
                                catch { }
                            }
                            break;
                    }

                    // Flush independently; ignore errors from disconnected clients.
                    try { await Response.Body.FlushAsync(CancellationToken.None); } catch { }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Generate-trip stream cancelled by client (backend continues via CancellationToken.None).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during generate-trip stream.");
                try { await WriteSSEAsync("error", ex.Message); } catch { }
                if (generatingUserId != Guid.Empty)
                {
                    // Track failure in cache for polling fallback
                    if (!string.IsNullOrEmpty(genId))
                        _generationTracking.SetFailed(genId, generatingUserId.ToString(), "Đã có lỗi xảy ra. Vui lòng thử lại.");

                    try
                    {
                        await _hubContext.Clients
                            .Group($"user-{generatingUserId}")
                            .SendAsync("TripGenerationFailed",
                                new { message = "Đã có lỗi xảy ra. Vui lòng thử lại." },
                                CancellationToken.None);
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Polling endpoint for checking background generation status.
        /// Uses [AllowAnonymous] because the generationId (UUID) itself serves as
        /// an authentication secret — only the client that initiated the generation
        /// knows the value. This avoids all token-expiry issues during page reload.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("generation-status/{generationId}")]
        public IActionResult GetGenerationStatus(string generationId)
        {
            var status = _generationTracking.GetStatus(generationId);
            if (status == null)
                return NotFound(new { status = "unknown" });

            return Ok(new { status = status.Status, tripId = status.TripId, error = status.Error });
        }

        [HttpPost("stream/modify-trip")]
        public async Task StreamModifyTrip(
            [FromBody] StreamModifyTripCommand command,
            CancellationToken cancellationToken)
        {
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            try
            {
                await foreach (var chunk in Mediator.CreateStream(command, cancellationToken))
                {
                    if (chunk.IsFailure)
                    {
                        await WriteSSEAsync("error", "Đã có lỗi xảy ra. Vui lòng thử lại.");
                        break;
                    }

                    switch (chunk.Value!.Type)
                    {
                        case StreamEventType.Start:
                            await WriteSSEAsync("start", JsonSerializer.Serialize(chunk.Value.Data));
                            break;

                        case StreamEventType.Parsing:
                            await WriteSSEAsync("parsing", JsonSerializer.Serialize(chunk.Value.Data));
                            break;

                        case StreamEventType.Chunk:
                            await WriteSSEAsync("chunk", chunk.Value.Data?.ToString() ?? "");
                            break;

                        case StreamEventType.Complete:
                            await WriteSSEAsync("complete", JsonSerializer.Serialize(chunk.Value.Data));
                            break;

                        case StreamEventType.Saving:
                            await WriteSSEAsync("saving", "Saving trip to database...");
                            break;

                        case StreamEventType.Saved:
                            await WriteSSEAsync("saved", JsonSerializer.Serialize(chunk.Value.Data));
                            break;
                    }

                    await Response.Body.FlushAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Stream cancelled by client");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during stream");
                await WriteSSEAsync("error", "Đã có lỗi xảy ra. Vui lòng thử lại.");
            }
        }

        private async Task WriteSSEAsync(string eventType, string data)
        {
            var dataLines = data.Split('\n');
            var formattedData = string.Join("\n", dataLines.Select(line => $"data: {line}"));
            var message = $"event: {eventType}\n{formattedData}\n\n";
            await Response.WriteAsync(message);
        }

        private async Task BroadcastAIEventToGroupAsync(
            Guid tripId,
            string eventName,
            object payload)
        {
            await _hubContext.Clients
                .Group($"trip-{tripId}")
                .SendAsync(eventName, payload, CancellationToken.None);
        }

        private async Task BroadcastToGroupAsync(
             Guid tripId,
             string? callerConnectionId,
             string eventName,
             object payload,
             CancellationToken cancellationToken)
        {
            var clients = !string.IsNullOrEmpty(callerConnectionId)
                ? _hubContext.Clients.GroupExcept($"trip-{tripId}", callerConnectionId)
                : _hubContext.Clients.Group($"trip-{tripId}");

            await clients.SendAsync(eventName, payload, cancellationToken);
        }
    }
}
