using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vivu.Application.UseCases.Destinations.Queries.GetTrendingDestinations;

namespace Vivu.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DestinationsController : ApiControllerBase
    {
        /// <summary>
        /// Get trending destinations based on public trips in the last 30 days
        /// </summary>
        /// <param name="query">Pagination parameters (PageNumber, PageSize, LocationsPerCity)</param>
        /// <returns>Paginated list of trending cities with their trending locations</returns>
        /// <response code="200">Returns the paginated list of trending destinations with locations</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("trending")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTrendingDestinations([FromQuery] GetTrendingDestinationsQuery query)
        {
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }
    }
}
