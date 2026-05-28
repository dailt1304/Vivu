using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vivu.Application.UseCases.TripDay.Commands.AddTripDay;
using Vivu.Application.UseCases.TripDay.Commands.RemoveTripDay;
using Vivu.Application.UseCases.TripDay.Commands.ReorderTripDay;
using Vivu.Application.UseCases.TripDay.Commands.UpdateTripDay;

namespace Vivu.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TripDayController : ApiControllerBase
    {

        /// <summary>
        /// Add trip day to trip
        /// </summary>
        /// <param name="command">The command containing tripId, title and dayDate</param>
        /// <returns>The created trip day with details</returns>
        [HttpPost]
        public async Task<IActionResult> AddTripDay([FromBody] AddTripDayCommand command)
        {
            var result = await Mediator.Send(command);

            if (result.IsSuccess)
                return CreatedAtAction(nameof(AddTripDay), new { id = result.Value.Id }, result);

            return HandleResult(result);
        }

        /// <summary>
        /// Update trip day
        /// </summary>
        /// <param name="tripDayId">The trip day ID to update</param>
        /// <param name="command">The command containing title and dayDate to update</param>
        /// <returns>The updated trip day with details</returns>
        [HttpPut("{tripDayId}")]
        public async Task<IActionResult> UpdateTripDay(Guid tripDayId, [FromBody] UpdateTripDayCommand command)
        {
            command.TripDayId = tripDayId;
            var result = await Mediator.Send(command);

            return HandleResult(result);
        }

        /// <summary>
        /// Remove trip day from trip
        /// </summary>
        /// <param name="tripDayId">The trip day ID to remove</param>
        /// <returns>The removed trip day details</returns>
        [HttpDelete("{tripDayId}")]
        public async Task<IActionResult> RemoveTripDay(Guid tripDayId)
        {
            var command = new RemoveTripDayCommand { TripDayId = tripDayId };
            var result = await Mediator.Send(command);

            return HandleResult(result);
        }

        /// <summary>
        /// Reorder trip days in a trip
        /// </summary>
        /// <param name="tripId">The trip ID</param>
        /// <param name="command">The command containing the ordered list of trip day IDs</param>
        /// <returns>The reordered list of trip days</returns>
        [HttpPut("reorder/{tripId}")]
        public async Task<IActionResult> ReorderTripDays([FromRoute] Guid tripId, [FromBody] ReorderTripDayCommand command)
        {
            command.TripId = tripId;
            var result = await Mediator.Send(command);

            return HandleResult(result);
        }
    }
}
