using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vivu.Application.UseCases.SubscriptionPackages.Commands.CreateSubscriptionPackage;
using Vivu.Application.UseCases.SubscriptionPackages.Commands.DeleteSubscriptionPackage;
using Vivu.Application.UseCases.SubscriptionPackages.Commands.SubscribeSubscriptionPackage;
using Vivu.Application.UseCases.SubscriptionPackages.Commands.UpdateSubscriptionPackage;
using Vivu.Application.UseCases.SubscriptionPackages.Queries.GetActiveSubscriptionPackages;
using Vivu.Application.UseCases.SubscriptionPackages.Queries.GetAllSubscriptionPackages;
using Vivu.Application.UseCases.SubscriptionPackages.Queries.GetSubscriptionPackageById;

namespace Vivu.WebApi.Controllers;

[Route("api/subscription-packages")]
[ApiController]
[Authorize]
public class SubscriptionPackagesController : ApiControllerBase
{
    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetAllSubscriptionPackages([FromQuery] GetAllSubscriptionPackagesQuery query)
    {
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveSubscriptionPackages()
    {
        var query = new GetActiveSubscriptionPackagesQuery();
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSubscriptionPackageById([FromRoute] Guid id)
    {
        var query = new GetSubscriptionPackageByIdQuery { Id = id };
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> CreateSubscriptionPackage(
        [FromBody] CreateSubscriptionPackageCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateSubscriptionPackage(
        Guid id,
        [FromBody] UpdateSubscriptionPackageCommand command)
    {
        command.Id = id;
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DeleteSubscriptionPackage(Guid id)
    {
        var command = new DeleteSubscriptionPackageCommand { Id = id };
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/subscribe")]
    public async Task<IActionResult> SubscribeSubscriptionPackage([FromRoute] Guid id)
    {
        var command = new SubscribeSubscriptionPackageCommand { PackageId = id };
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}
