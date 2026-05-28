using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.UseCases.Trips.Queries.GetUserFavoriteTrips;
using Vivu.Application.UseCases.Users.Commands.BanUser;
using Vivu.Application.UseCases.Users.Commands.CreateUser;
using Vivu.Application.UseCases.Users.Commands.UnbanUser;
using Vivu.Application.UseCases.Users.Commands.UpdateUserProfile;
using Vivu.Application.UseCases.Users.Queries.GetAllUsers;
using Vivu.Application.UseCases.Users.Queries.GetUsageStats;
using Vivu.Application.UseCases.Users.Queries.GetUserById;
using Vivu.Application.UseCases.Users.Queries.SearchUsers;
using Vivu.Domain.Entities;

namespace Vivu.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ApiControllerBase
    {
        [HttpPost]
        [Authorize(Roles = Role.Names.Admin)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command)
        {
            var result = await Mediator.Send(command);

            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result);
            }

            return BadRequest(result);
        }

        [HttpGet]
        [Authorize(Roles = $"{Role.Names.Admin},{Role.Names.Moderator}")]
        public async Task<IActionResult> GetAll([FromQuery] GetAllUsersQuery request)
        {
            var result = await Mediator.Send(request);
            return HandleResult(result);
        }

        [HttpGet("search")]
        [Authorize(Roles = $"{Role.Names.Admin}")]
        public async Task<IActionResult> SearchUsers([FromQuery] SearchUsersQuery request)
        {
            var result = await Mediator.Send(request);
            return HandleResult(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var result = await Mediator.Send(new GetUserByIdQuery { UserId = id });
            return HandleResult(result);
        }

        [HttpGet("me/favorites")]
        public async Task<IActionResult> GetMyFavoriteTrips([FromQuery] GetUserFavoriteTripsQuery request)
        {
            var result = await Mediator.Send(request);
            return HandleResult(result);
        }

        [HttpGet("usage")]
        public async Task<IActionResult> GetUsageStats(CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(new GetUsageStatsQuery(), cancellationToken);
            return HandleResult(result);
        }

        [HttpPut("me/profile")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileCommand command)
        {
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("me/avatar")]
        public async Task<IActionResult> UploadAvatar([FromForm] Vivu.Application.UseCases.Users.Commands.UploadAvatar.UploadAvatarCommand command)
        {
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("{id:guid}/ban")]
        [Authorize(Roles = $"{Role.Names.Admin}")]
        public async Task<IActionResult> BanUser([FromRoute] Guid id, [FromBody] BanUserCommand command)
        {
            command.UserId = id;
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("{id:guid}/unban")]
        [Authorize(Roles = $"{Role.Names.Admin}")]
        public async Task<IActionResult> UnbanUser([FromRoute] Guid id)
        {
            var result = await Mediator.Send(new UnbanUserCommand { UserId = id });
            return HandleResult(result);
        }
    }
}