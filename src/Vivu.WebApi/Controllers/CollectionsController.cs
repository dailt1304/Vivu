using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vivu.Application.UseCases.Collections.Commands.AddLocationToCollection;
using Vivu.Application.UseCases.Collections.Commands.CreateCollection;
using Vivu.Application.UseCases.Collections.Commands.DeleteCollection;
using Vivu.Application.UseCases.Collections.Commands.RemoveLocationFromCollection;
using Vivu.Application.UseCases.Collections.Commands.UpdateCollection;
using Vivu.Application.UseCases.Collections.Queries.GetCollectionDetail;
using Vivu.Application.UseCases.Collections.Queries.GetUserCollections;
using Vivu.Application.UseCases.Collections.Queries.GetUserCollectionsSummary;

namespace Vivu.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CollectionsController : ApiControllerBase
    {
        private Guid? GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return null;
            return userId;
        }

        // GET /api/collections?pageNumber=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> GetCollections(
            [FromQuery] GetUserCollectionsQuery query,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "Invalid or missing user token." });

            query.UserId = userId.Value;

            var result = await Mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        // GET /api/collections/summary
        [HttpGet("summary")]
        public async Task<IActionResult> GetCollectionsSummary(
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "Invalid or missing user token." });

            var query = new GetUserCollectionsSummaryQuery();
            query.UserId = userId.Value;

            var result = await Mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        // GET /api/collections/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetCollectionDetail(
            [FromRoute] Guid id,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "Invalid or missing user token." });

            var query = new GetCollectionDetailQuery();
            query.CollectionId = id;
            query.UserId = userId.Value;

            var result = await Mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        // POST /api/collections (multipart/form-data)
        [HttpPost]
        public async Task<IActionResult> CreateCollection(
            [FromForm] CreateCollectionCommand command,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "Invalid or missing user token." });

            command.UserId = userId.Value;

            var result = await Mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        // PUT /api/collections/{id} (multipart/form-data)
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateCollection(
            [FromRoute] Guid id,
            [FromForm] UpdateCollectionCommand command,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "Invalid or missing user token." });

            command.CollectionId = id;
            command.UserId = userId.Value;

            var result = await Mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        // DELETE /api/collections/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteCollection(
            [FromRoute] Guid id,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "Invalid or missing user token." });

            var command = new DeleteCollectionCommand
            {
                CollectionId = id,
                UserId = userId.Value
            };

            var result = await Mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        // POST /api/collections/{id}/locations
        [HttpPost("{id:guid}/locations")]
        public async Task<IActionResult> AddLocationToCollection(
            [FromRoute] Guid id,
            [FromBody] AddLocationToCollectionCommand command,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "Invalid or missing user token." });

            command.CollectionId = id;
            command.UserId = userId.Value;

            var result = await Mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        // DELETE /api/collections/{id}/locations/{locationId}
        [HttpDelete("{id:guid}/locations/{locationId:guid}")]
        public async Task<IActionResult> RemoveLocationFromCollection(
            [FromRoute] Guid id,
            [FromRoute] Guid locationId,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "Invalid or missing user token." });

            var command = new RemoveLocationFromCollectionCommand
            {
                CollectionId = id,
                LocationId = locationId,
                UserId = userId.Value
            };

            var result = await Mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }
    }
}
