using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Vivu.Application.Common.Behaviors;

namespace Vivu.WebApi.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IHostEnvironment _env;
        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            if (exception is ValidationException validationException)
            {
                httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;

                var problemDetailsEx = new ProblemDetails
                {
                    Status = StatusCodes.Status422UnprocessableEntity, 
                    Title = "Validation Failed",
                    Detail = "One or more validation errors occurred.",
                    Type = "https://datatracker.ietf.org/doc/html/rfc4918#section-11.2"
                };
                problemDetailsEx.Extensions.Add("errors", validationException.Errors);

                problemDetailsEx.Extensions.Add("timestamp", DateTime.UtcNow);

                problemDetailsEx.Extensions.Add("traceId", httpContext.TraceIdentifier);

                _logger.LogWarning(
                    "Validation error occurred. TraceId: {TraceId}, Errors: {@Errors}",
                    httpContext.TraceIdentifier,
                    validationException.Errors);

                await httpContext.Response.WriteAsJsonAsync(problemDetailsEx, cancellationToken);

                return true; 
            }

            _logger.LogError(
            exception,
            "An unhandled exception occurred. TraceId: {TraceId}",
            httpContext.TraceIdentifier);

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
                Extensions =
                    {
                        ["traceId"] = httpContext.TraceIdentifier
                    }
            };


            if (_env.IsDevelopment())
            {
                problemDetails.Detail = exception.Message;
                problemDetails.Extensions["stackTrace"] = exception.StackTrace; 
            }
            else
            {
                problemDetails.Detail = "An unexpected error occurred. Please contact support with the TraceId.";
            }

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;

        }
    }
}
