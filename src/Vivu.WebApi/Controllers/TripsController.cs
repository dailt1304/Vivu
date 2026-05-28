using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Vivu.Application.UseCases.Trips.Commands.DeleteTrip;
using Vivu.Application.UseCases.Trips.Commands.UpdateTrip;
using Vivu.Application.UseCases.Trips.Commands.UpdateTripVisibility;
using Vivu.Application.UseCases.Trips.Commands.CreateTrip;
using Vivu.Application.UseCases.Trips.Commands.RateTrip;
using Vivu.Application.UseCases.Trips.Commands.UpdateTripStatus;
using Vivu.Application.UseCases.Trips.Commands.UpdateMemberRole;
using Vivu.Application.UseCases.Trips.Queries.GetCompletedTrips;
using Vivu.Application.UseCases.Trips.Queries.GetPublicTripDetail;
using Vivu.Application.UseCases.Trips.Queries.GetPublicTrips;
using Vivu.Application.UseCases.Trips.Queries.GetTripById;
using Vivu.Application.UseCases.Trips.Queries.GetUserTrips;
using Vivu.Application.UseCases.Trips.Queries.SearchPublicTrips;
using Vivu.Domain.Entities;
using Vivu.Application.UseCases.Trips.Commands.JoinTripByCode;
using Vivu.Application.UseCases.Trips.Commands.FavouriteTrip;
using Vivu.Application.UseCases.Trips.Commands.UnfavoriteTrip;
using Vivu.Application.Common.Models;
using Vivu.Application.UseCases.Trips.Queries.GetTripChatHistory;
using Vivu.Application.UseCases.Trips.Commands.CopyTrip;
using Vivu.Application.UseCases.TripImages.Queries.GetTripImages;
using Vivu.Application.UseCases.ChatMessages.Commands.SendChatMessage;
using Vivu.Application.UseCases.ChatMessages.Commands.DeleteChatMessage;
using Vivu.WebApi.Hubs;

namespace Vivu.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TripsController : ApiControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateTrip([FromBody] CreateTripCommand command)
        {
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpGet("user/{userId:guid}")]
        public async Task<IActionResult> GetUserTrips(
            [FromRoute] Guid userId,
            [FromQuery] string? status = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetUserTripsQuery
            {
                UserId = userId,
                Status = status,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetTripById([FromRoute] Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid or missing user token." });
            }

            var query = new GetTripByIdQuery
            {
                TripId = id,
                RequestUserId = userId
            };

            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinTripByCode([FromBody] JoinTripByCodeCommand command)
        {
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpGet("{tripId}/messages")]
        public async Task<IActionResult> GetChatHistory(
                [FromRoute] Guid tripId,
                [FromQuery] PaginationRequest pagination)
        {
            var result = await Mediator.Send(new GetTripChatHistoryQuery
            {
                TripId = tripId,
                PageNumber = pagination.PageNumber,
                PageSize = pagination.PageSize,
                SortColumn = pagination.SortColumn,
                SortDescending = pagination.SortDescending
            });
            return HandleResult(result);
        }

        [HttpGet("completed/{userId:guid}")]
        public async Task<IActionResult> GetCompletedTrips(
            [FromRoute] Guid userId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetCompletedTripsQuery
            {
                UserId = userId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpGet("public/{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicTripDetail([FromRoute] Guid id)
        {
            var query = new GetPublicTripDetailQuery
            {
                TripId = id
            };

            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpGet("public")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicTrips(
            [FromQuery] Guid? city = null,
            [FromQuery] Guid? country = null,
            [FromQuery] int? duration = null,
            [FromQuery] string? sortBy = "newest",
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetPublicTripsQuery
            {
                CityId = city,
                CountryId = country,
                Duration = duration,
                SortBy = sortBy,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpGet("public/search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchPublicTrips(
            [FromQuery] string q,
            [FromQuery] Guid? city = null,
            [FromQuery] Guid? country = null,
            [FromQuery] int? duration = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new SearchPublicTripsQuery
            {
                SearchTerm = q,
                CityId = city,
                CountryId = country,
                Duration = duration,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpPost("{id:guid}/rate")]
        public async Task<IActionResult> RateTrip(
            [FromRoute] Guid id,
            [FromBody] RateTripCommand command)
        {
            command.TripId = id;
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }
            
        [HttpPost("update-statuses")]
        public async Task<IActionResult> UpdateTripStatuses()
        {
            var command = new UpdateTripStatusCommand();
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateTrip(
            [FromRoute] Guid id,
            [FromBody] UpdateTripCommand command)
        {
            command.TripId = id;
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPatch("{tripId:guid}/visibility")]
        public async Task<IActionResult> UpdateTripVisibility(
            [FromRoute] Guid tripId,
            [FromBody] UpdateTripVisibilityCommand command)
        {
            command.TripId = tripId;
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPatch("{tripId:guid}/members/{memberUserId:guid}")]
        public async Task<IActionResult> UpdateMemberRole(
            [FromRoute] Guid tripId,
            [FromRoute] Guid memberUserId,
            [FromBody] UpdateMemberRoleCommand command)
        {
            command.TripId = tripId;
            command.MemberUserId = memberUserId;
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("{id:guid}/copy")]
        public async Task<IActionResult> CopyTrip(
            [FromRoute] Guid id,
            [FromBody] CopyTripCommand command)
        {
            command.TripId = id;
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTrip([FromRoute] Guid id)
        {
            var command = new DeleteTripCommand
            {
                TripId = id
            };

            var result = await Mediator.Send(command);
            return HandleResult(result);
        }
        [HttpPost("{tripId}/favorite")]
        public async Task<IActionResult> FavoriteTrip(Guid tripId)
        {
            var command = new FavoriteTripCommand(tripId);
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }


        [HttpDelete("{tripId}/favorite")]
        public async Task<IActionResult> UnfavoriteTrip(Guid tripId)
        {
            var command = new UnfavoriteTripCommand(tripId);
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpGet("{tripId:guid}/images")]
        public async Task<IActionResult> GetTripImages([FromRoute] Guid tripId)
        {
            var query = new GetTripImagesQuery { TripId = tripId };
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpPost("{tripId:guid}/chat/images")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SendChatImage(
            [FromRoute] Guid tripId,
            [FromForm] SendChatImageRequest request)
        {
            var command = new SendChatMessageCommand
            {
                TripId = tripId,
                Content = request.Content ?? string.Empty,
                MessageType = "image",
                Image = request.Image
            };

            var result = await Mediator.Send(command);

            if (result.IsSuccess)
            {
                // Broadcast to SignalR group so other connected clients see the message
                var hubContext = HttpContext.RequestServices
                    .GetRequiredService<IHubContext<TripChatHub>>();
                await hubContext.Clients.Group($"trip-{tripId}")
                    .SendAsync("ReceiveMessage", result.Value);
            }

            return HandleResult(result);
        }

        [HttpDelete("{tripId:guid}/messages/{messageId:guid}")]
        public async Task<IActionResult> DeleteChatMessage(
            [FromRoute] Guid tripId,
            [FromRoute] Guid messageId)
        {
            var command = new DeleteChatMessageCommand
            {
                TripId = tripId,
                MessageId = messageId
            };

            var result = await Mediator.Send(command);

            if (result.IsSuccess)
            {
                var hubContext = HttpContext.RequestServices
                    .GetRequiredService<IHubContext<TripChatHub>>();
                await hubContext.Clients.Group($"trip-{tripId}")
                    .SendAsync("MessageDeleted", new { messageId, tripId });
            }

            return HandleResult(result);
        }
    }

    /// <summary>
    /// Wrapper DTO so Swagger can generate the multipart/form-data schema correctly.
    /// </summary>
    public class SendChatImageRequest
    {
        public IFormFile Image { get; set; } = null!;
        public string? Content { get; set; }
    }
}
