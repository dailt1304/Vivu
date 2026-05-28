using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vivu.Application.UseCases.TripLocation.Commands.AddAlternative;
using Vivu.Application.UseCases.TripLocation.Commands.AddLocationToTrip;
using Vivu.Application.UseCases.TripLocation.Commands.RemoveAlternative;
using Vivu.Application.UseCases.TripLocation.Commands.RemoveTripLocation;
using Vivu.Application.UseCases.TripLocation.Commands.ReorderTripLocations;
using Vivu.Application.UseCases.TripLocation.Commands.SwapToPrimary;
using Vivu.Application.UseCases.TripLocation.Commands.UpdateTripLocation;

namespace Vivu.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TripLocationController : ApiControllerBase
    {
        /// <summary>
        /// Add a location to a trip day
        /// </summary>
        /// <param name="command">The command containing trip day, location, and schedule information</param>
        /// <returns>The created trip location with details</returns>
        [HttpPost]
        public async Task<IActionResult> AddLocationToTrip([FromBody] AddLocationToTripCommand command)
        {
            var result = await Mediator.Send(command);

            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(AddLocationToTrip), new { id = result.Value.Id }, result);
            }

            return HandleFailure(result);
        }

        /// <summary>
        /// Update an existing trip location
        /// </summary>
        /// <param name="id">The trip location ID</param>
        /// <param name="command">The command containing updated trip location information</param>
        /// <returns>The updated trip location with details</returns>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateTripLocation([FromRoute] Guid id, [FromBody] UpdateTripLocationCommand command)
        {
            command.TripLocationId = id;
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Remove a location from a trip day
        /// </summary>
        /// <param name="id">The trip location ID to remove</param>
        /// <returns>The removed trip location details</returns>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> RemoveTripLocation([FromRoute] Guid id)
        {
            var command = new RemoveTripLocationCommand { TripLocationId = id };
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Reorder trip locations in a trip day
        /// </summary>
        /// <param name="command">The command containing trip day ID and ordered list of trip location IDs</param>
        /// <returns>The reordered list of trip locations</returns>
        [HttpPut("reorder")]
        public async Task<IActionResult> ReorderTripLocations([FromBody] ReorderTripLocationsCommand command)
        {
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        // ===== Alternative Locations Endpoints =====

        /// <summary>
        /// Add an alternative (backup) location to a trip location
        /// </summary>
        /// <param name="id">The primary trip location ID</param>
        /// <param name="command">The command containing the alternative location ID and optional reason</param>
        /// <returns>The created alternative location details</returns>
        [HttpPost("{id:guid}/alternatives")]
        public async Task<IActionResult> AddAlternative(
            [FromRoute] Guid id, [FromBody] AddAlternativeCommand command)
        {
            command.TripLocationId = id;
            var result = await Mediator.Send(command);

            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(AddAlternative), new { id = result.Value.Id }, result);
            }

            return HandleFailure(result);
        }

        /// <summary>
        /// Remove an alternative location
        /// </summary>
        /// <param name="alternativeId">The alternative ID to remove</param>
        /// <returns>Success or failure</returns>
        [HttpDelete("alternatives/{alternativeId:guid}")]
        public async Task<IActionResult> RemoveAlternative([FromRoute] Guid alternativeId)
        {
            var command = new RemoveAlternativeCommand { AlternativeId = alternativeId };
            var result = await Mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(new { success = true, message = "Alternative removed successfully." });
            }

            return HandleFailure(result);
        }

        /// <summary>
        /// Swap an alternative location to become the primary location
        /// </summary>
        /// <param name="id">The trip location ID</param>
        /// <param name="alternativeId">The alternative ID to promote</param>
        /// <returns>The updated trip location with swapped primary</returns>
        [HttpPost("{id:guid}/alternatives/{alternativeId:guid}/swap")]
        public async Task<IActionResult> SwapToPrimary(
            [FromRoute] Guid id, [FromRoute] Guid alternativeId)
        {
            var command = new SwapToPrimaryCommand
            {
                TripLocationId = id,
                AlternativeId = alternativeId
            };
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }
    }
}

