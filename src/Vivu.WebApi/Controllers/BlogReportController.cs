using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vivu.Application.UseCases.BlogReports.Commands.ReviewBlogReport;
using Vivu.Application.UseCases.BlogReports.Queries.GetBlogReports;

namespace Vivu.WebApi.Controllers
{
    [Route("api/blog-reports")]
    [ApiController]
    public class BlogReportController : ApiControllerBase
    {
        [HttpGet]
        [Authorize(Roles = "MODERATOR")]
        public async Task<IActionResult> GetReports([FromQuery] GetBlogReportsQuery query)
        {
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpPut("review")]
        [Authorize(Roles = "MODERATOR")]
        public async Task<IActionResult> ReviewReport([FromBody] ReviewBlogReportCommand command)
        {
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }
    }
}
