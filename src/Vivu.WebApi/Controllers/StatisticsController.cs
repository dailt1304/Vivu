using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vivu.Application.UseCases.Statictis.Queries.GetLocationStats;
using Vivu.Application.UseCases.Statictis.Queries.GetAdminDashboard;

namespace Vivu.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StatisticsController : ApiControllerBase
    {
        [HttpGet("locations")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetLocationStats()
        {
            var query = new GetLocationStatsQuery();
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpGet("dashboard")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var query = new GetAdminDashboardQuery();
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }
    }
}
