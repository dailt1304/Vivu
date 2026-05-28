using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vivu.Application.UseCases.Cities.Commands.CreateCity;
using Vivu.Application.UseCases.Cities.Commands.DeleteCity;
using Vivu.Application.UseCases.Cities.Commands.UpdateCity;
using Vivu.Application.UseCases.Cities.Queries.GetAllCities;
using Vivu.Application.UseCases.Cities.Queries.GetCityById;
using Vivu.Application.UseCases.Cities.Queries.SearchCities;

namespace Vivu.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CitiesController : ApiControllerBase
    {
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll([FromQuery] GetAllCitiesQuery query)
        {
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpGet("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var result = await Mediator.Send(new GetCityByIdQuery { CityId = id });
            return HandleResult(result);
        }

        [HttpGet("search")]
        [Authorize]
        public async Task<IActionResult> Search([FromQuery] SearchCitiesQuery query)
        {
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,MODERATOR")]
        public async Task<IActionResult> Create([FromBody] CreateCityCommand command)
        {
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "ADMIN,MODERATOR")]
        public async Task<IActionResult> Update(
            [FromRoute] Guid id, [FromBody] UpdateCityCommand command)
        {
            command.CityId = id;
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var result = await Mediator.Send(new DeleteCityCommand { CityId = id });
            return HandleResult(result);
        }
    }
}
