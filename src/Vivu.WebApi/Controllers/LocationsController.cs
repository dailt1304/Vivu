using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.UseCases.Locations.Commands.CreateLocation;
using Vivu.Application.UseCases.Locations.Commands.DeleteLocation;
using Vivu.Application.UseCases.Locations.Commands.RejectLocationSuggestion;
using Vivu.Application.UseCases.Locations.Commands.ApproveLocationSuggestion;
using Vivu.Application.UseCases.Locations.Commands.ReportLocation;
using Vivu.Application.UseCases.Locations.Commands.SubmitNewLocation;
using Vivu.Application.UseCases.Locations.Commands.UpdateLocation;
using Vivu.Application.UseCases.Locations.Commands.UpdateLocationSuggestion;
using Vivu.Application.UseCases.Locations.Queries.GetAllLocations;
using Vivu.Application.UseCases.Locations.Queries.GetLocatioinById;
using Vivu.Application.UseCases.Locations.Queries.GetLocationBySearchText;
using Vivu.Application.UseCases.Locations.Queries.GetLocationsWithFilters;
using Vivu.Application.UseCases.Locations.Queries.GetNearbyLocations;
using Vivu.Application.UseCases.Locations.Queries.GetPendingLocations;
using Vivu.Application.UseCases.Locations.Queries.GetPopularLocations;
using Vivu.Application.UseCases.Locations.Queries.GetUserSubmittedLocations;

namespace Vivu.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LocationsController : ApiControllerBase
    {
        /// <summary>
        /// Get all locations with pagination
        /// </summary>
        /// <param name="query">Pagination parameters (PageNumber, PageSize)</param>
        /// <returns>Paginated list of locations with details including City, Category, and LocationDetail</returns>
        /// <response code="200">Returns the paginated list of locations</response>
        /// <response code="401">Unauthorized - Invalid or missing authentication token</response>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetAllLocationsQuery query)
        {
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpGet("get-by-filter")]
        public async Task<IActionResult> GetLocations([FromQuery] GetLocationsByFilterQuery query)
        {
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }
        /// <summary>
        /// Submit a new location with image uploads
        /// </summary>
        /// <param name="command">New location data including name, address, coordinates, and optional image files</param>
        /// <returns>The created location</returns>
        /// <response code="200">Location created successfully</response>
        /// <response code="400">Invalid request data or image upload failed</response>
        /// <response code="401">Unauthorized - Invalid or missing authentication token</response>
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SubmitNewLocation([FromForm] SubmitNewLocationCommand command)
        {
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Create a new location directly (Moderator/Admin only). Auto-verified, no report needed.
        /// </summary>
        /// <param name="command">New location data including name, address, coordinates, and optional image files</param>
        /// <returns>The created location</returns>
        /// <response code="200">Location created successfully with isVerified=true</response>
        /// <response code="400">Invalid request data or duplicate location</response>
        /// <response code="401">Unauthorized - Invalid or missing authentication token</response>
        /// <response code="403">Forbidden - User does not have required role (ADMIN or MODERATOR)</response>
        [HttpPost("create")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "ADMIN,MODERATOR")]
        public async Task<IActionResult> CreateLocation([FromForm] CreateLocationCommand command)
        {
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }
            /// Get locations that the current user has reported, with pagination and optional report status filter
            /// </summary>
            /// <param name="query">Query parameters (PageNumber, PageSize, Status: PENDING/APPROVED/REJECTED)</param>
            /// <returns>Paginated list of locations that the authenticated user has reported, filtered by report status if specified</returns>
            /// <response code="200">Returns the paginated list of locations user has reported</response>
            /// <response code="401">Unauthorized - Invalid or missing authentication token</response>
            [HttpGet("my-submissions")]
        public async Task<IActionResult> GetUserSubmitted([FromQuery] GetUserSubmittedLocationsQuery query)
        {
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Get all locations with pending reports
        /// </summary>
        /// <param name="query">Pagination parameters (PageNumber, PageSize)</param>
        /// <returns>Paginated list of locations that have PENDING status reports from LocationReports</returns>
        /// <response code="200">Returns the paginated list of pending locations</response>
        /// <response code="401">Unauthorized - Invalid or missing authentication token</response>
        /// <response code="403">Forbidden - User does not have required role (ADMIN or MODERATOR)</response>
        [HttpGet("pending")]
        [Authorize(Roles = "ADMIN,MODERATOR")]
        public async Task<IActionResult> GetPendingLocations([FromQuery] GetPendingLocationsQuery query)
        {
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpPost("report")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Report([FromForm] ReportLocationCommand command)
        {
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("search-term")]
        public async Task<IActionResult> SearchTermLocation([FromQuery] SearchLocationsQuery query)
        {
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "MODERATOR")]
        public async Task<IActionResult> Update(
            [FromRoute] Guid id,
            [FromBody] UpdateLocationCommand command)
        {
            command.LocationId = id;
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Submit an update suggestion for an existing location
        /// </summary>
        /// <param name="id">Location ID</param>
        /// <param name="command">Update suggestion including changes and reason</param>
        /// <returns>The current location data</returns>
        /// <response code="200">Update suggestion submitted successfully</response>
        /// <response code="400">Invalid request data or image upload failed</response>
        /// <response code="401">Unauthorized - Invalid or missing authentication token</response>
        /// <response code="404">Location not found</response>
        [HttpPut("{id:guid}/suggest-update")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SuggestUpdate(
            [FromRoute] Guid id,
            [FromForm] UpdateLocationSuggestionCommand command)
        {
            command.LocationId = id;
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLocationById(Guid id)
        {
            var query = new GetLocationByIdQuery(id);
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "MODERATOR")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var command = new DeleteLocationCommand { LocationId = id };
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }
        [HttpGet("popular")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPopularLocations([FromQuery] GetPopularLocationsQuery query)
        {
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }
        [HttpGet("nearby")]
        public async Task<IActionResult> GetNearbyLocations([FromQuery] GetNearbyLocationsQuery query)
        {
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Reject a location suggestion by updating all pending NEW_LOCATION reports to REJECTED status
        /// </summary>
        /// <param name="id">Location ID</param>
        /// <param name="command">Command containing admin note</param>
        /// <returns>The location details</returns>
        /// <response code="200">Location suggestion rejected successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Unauthorized - Invalid or missing authentication token</response>
        /// <response code="403">Forbidden - User does not have required role (MODERATOR)</response>
        /// <response code="404">Location not found or no pending reports found</response>
        [HttpPost("{id:guid}/reject-suggestion")]
        [Authorize(Roles = "ADMIN,MODERATOR")]
        public async Task<IActionResult> RejectLocationSuggestion(
            [FromRoute] Guid id,
            [FromBody] RejectLocationSuggestionCommand command)
        {
            command.LocationId = id;
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        /// Approve a location suggestion (NEW_LOCATION report)
        /// </summary>
        /// <param name="id">Location ID</param>
        /// <param name="command">Approval details including optional admin note</param>
        /// <returns>The approved location with isVerified set to true</returns>
        /// <response code="200">Location approved successfully</response>
        /// <response code="404">Location not found or no pending report found</response>
        /// <response code="401">Unauthorized - Invalid or missing authentication token</response>
        /// <response code="403">Forbidden - User does not have ADMIN or MODERATOR role</response>
        [HttpPut("{id:guid}/approve")]
        [Authorize(Roles = "ADMIN,MODERATOR")]
        public async Task<IActionResult> ApproveLocationSuggestion(
            [FromRoute] Guid id,
            [FromBody] ApproveLocationSuggestionCommand command)
        {
            command.LocationId = id;
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }
    }
}
