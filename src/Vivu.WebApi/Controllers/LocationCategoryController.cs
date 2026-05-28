using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vivu.Application.UseCases.Locations.Queries.GetLocationCategories;
using Vivu.Application.UseCases.LocationCategories.Commands.CreateCategory;
using Vivu.Application.UseCases.LocationCategories.Commands.UpdateCategory;
using Vivu.Application.UseCases.LocationCategories.Commands.DeleteCategory;
using Vivu.Application.UseCases.LocationCategories.Commands.ChangeCategoryStatus;

namespace Vivu.WebApi.Controllers
{
    [Route("api/location-categories")]
    [ApiController]
    [Authorize]
    public class LocationCategoryController : ApiControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetLocationCategoriesQuery query)
        {
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,MODERATOR")]
        public async Task<IActionResult> Create([FromBody] CreateCategoryCommand command)
        {
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "ADMIN,MODERATOR")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryCommand command)
        {
            command.Id = id;
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN,MODERATOR")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await Mediator.Send(new DeleteCategoryCommand { Id = id });
            return HandleResult(result);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "ADMIN,MODERATOR")]
        public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeCategoryStatusCommand command)
        {
            command.Id = id;
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }
    }
}
