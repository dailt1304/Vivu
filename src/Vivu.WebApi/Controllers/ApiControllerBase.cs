using System.Net;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vivu.Domain.Shared;

namespace Vivu.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        private ISender? _mediator;

        protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

        protected IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                return Ok(new { success = true, data = result.Value });
            }

            return HandleFailure(result);
        }

        protected IActionResult HandleFailure(Result result)
        {
            var statusCode = GetStatusCode(result.Error.Code);

            var response = new
            {
                success = false,
                statusCode,
                message = result.Error.Message,
                code = result.Error.Code,
                errors = result.Error.ValidationErrors,
                timestamp = DateTime.UtcNow
            };
            return StatusCode(statusCode, response);
        }

        private static int GetStatusCode(string errorCode) => errorCode switch
        {
            "Validation.Error" => StatusCodes.Status422UnprocessableEntity,
            string code when code.Contains(".NotFound") => 404,
            string code when code.StartsWith("Auth.TooManyOtpRequests") => (int)HttpStatusCode.TooManyRequests,
            string code when code.StartsWith("Auth.Otp") => 400,
            string code when code.StartsWith("Auth.") => 401,
            string code when code.Contains(".AccessDenied") || code.Contains(".Banned") => 403,
            string code when code.Contains("AlreadyExists") || code.Contains("Already") => 409,
            string code when code.Contains("Invalid") || code.Contains("Expired") => (int)HttpStatusCode.BadRequest,
            "Subscription.LimitReached" => 429,
            _ => 400
        };

    }
}
