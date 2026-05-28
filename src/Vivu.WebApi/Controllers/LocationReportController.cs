using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vivu.Application.UseCases.LocationReports.Commands.ReviewLocationReport;
using Vivu.Application.UseCases.LocationReports.Queries.GetLocationReports;
using Vivu.Application.UseCases.LocationReports.Queries.GetMyLocationReports;

namespace Vivu.WebApi.Controllers
{
    [Route("api/location-reports")]
    [ApiController]
    public class LocationReportController : ApiControllerBase
    {
        /// <summary>
        /// Get the current user's own location reports with pagination and optional filters
        /// </summary>
        [HttpGet("my-reports")]
        [Authorize]
        public async Task<IActionResult> GetMyReports([FromQuery] GetMyLocationReportsQuery query)
        {
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpGet]
        [Authorize(Roles = "MODERATOR")]
        public async Task<IActionResult> GetReports([FromQuery] GetLocationReportsQuery query)
        {
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpPut("review")]
        [Authorize(Roles = "MODERATOR")]
        public async Task<IActionResult> ReviewReport([FromBody] ReviewLocationReportCommand command)
        {
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Update an existing pending location report
        /// </summary>
        /// <param name="id">Report ID</param>
        /// <param name="command">Update details including reason, type, and optional images</param>
        /// <returns>The updated location report</returns>
        /// <response code="200">Location report updated successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Report not found</response>
        [HttpPut("{id:guid}")]
        [Consumes("multipart/form-data")]
        [Authorize]
        public async Task<IActionResult> UpdateReport([FromRoute] Guid id, [FromForm] Vivu.Application.UseCases.LocationReports.Commands.UpdateLocationReport.UpdateLocationReportCommand command)
        {
            command.ReportId = id;
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }
    }
}
