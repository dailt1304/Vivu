using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vivu.Application.UseCases.TripMember.Command.RemoveMemberFromTrip;
using Vivu.Application.UseCases.Trips.Commands.JoinTripByCode;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vivu.Application.UseCases.TripMembers.Commands.LeaveTrip;
using Vivu.Application.UseCases.TripMembers.Queries.GetTripMembers;

namespace Vivu.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TripMembersController : ApiControllerBase
    {
        [HttpPost("remove-member")]
        public async Task<IActionResult> RemoveMemberFromTrip([FromBody] RemoveMemberFromTripCommand command)
        {
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }
        [HttpGet("{tripId:guid}")]
        public async Task<IActionResult> GetTripMembers(
            [FromRoute] Guid tripId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetTripMembersQuery
            {
                TripId = tripId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpDelete("{tripId:guid}/leave")]
        public async Task<IActionResult> LeaveTrip([FromRoute] Guid tripId)
        {
            var command = new LeaveTripCommand
            {
                TripId = tripId
            };

            var result = await Mediator.Send(command);
            return HandleResult(result);
        }
    }
}
