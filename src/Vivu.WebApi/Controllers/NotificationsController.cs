using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Vivu.Application.UseCases.Notifications.Commands.CreateNotification;
using Vivu.Application.UseCases.Notifications.Commands.DeleteNotification;
using Vivu.Application.UseCases.Notifications.Commands.MarkAllAsRead;
using Vivu.Application.UseCases.Notifications.Commands.MarkNotificationAsRead;
using Vivu.Application.UseCases.Notifications.Queries.GetUnreadCount;
using Vivu.Application.UseCases.Notifications.Queries.GetUserNotifications;

namespace Vivu.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ApiControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationCommand command)
        {
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetUserNotifications(
            [FromQuery] bool? unreadOnly = null,
            [FromQuery] string? type = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid or missing user token." });
            }

            var query = new GetUserNotificationsQuery
            {
                UserId = userId,
                UnreadOnly = unreadOnly,
                Type = type,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var result = await Mediator.Send(new GetUnreadCountQuery());
            return HandleResult(result);
        }

        [HttpPatch("{id:guid}/read")]
        public async Task<IActionResult> MarkAsRead([FromRoute] Guid id)
        {
            var command = new MarkNotificationAsReadCommand { NotificationId = id };
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var result = await Mediator.Send(new MarkAllAsReadCommand());
            return HandleResult(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteNotification([FromRoute] Guid id)
        {
            var command = new DeleteNotificationCommand { NotificationId = id };
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }
    }
}
