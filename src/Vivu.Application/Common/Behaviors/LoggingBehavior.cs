using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Shared;

namespace Vivu.Application.Common.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
        private readonly ICurrentUser _currentUser;
        private readonly IHostEnvironment _env;

        public LoggingBehavior(
            ILogger<LoggingBehavior<TRequest, TResponse>> logger,
           IHostEnvironment env, ICurrentUser currentUser)
        {
            _logger = logger;
            _currentUser = currentUser;
            _env = env;
        }

        public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;

            if (ShouldExclude(requestName)) return await next();

            var userId = _currentUser.Id ?? "Anonymous";
            var traceId = _currentUser.TraceId ?? Guid.NewGuid().ToString();

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestName"] = requestName,
                ["UserId"] = userId,
                ["TraceId"] = traceId
            }))
            {
                if (_env.IsDevelopment() )
                {
                    _logger.LogInformation("Handling {RequestName} by user {UserId}. Request: {@Request}",
                                            requestName,
                                            userId,
                                            SanitizeRequest(request));
                }

                var timer = Stopwatch.StartNew();

                try
                {
                    var response = await next();

                    timer.Stop();
                    var elapsedMs = timer.ElapsedMilliseconds;

                    if (response.IsFailure)
                    {
                        _logger.LogWarning(
                            "LOGIC ERROR: {RequestName} failed in {ElapsedMs}ms. Error: {ErrorCode} - {ErrorMessage}",
                            requestName, elapsedMs, response.Error.Code, response.Error.Message);
                    }
                    else
                    {
                        if (elapsedMs > 3000)
                            _logger.LogWarning("SLOW REQUEST: {RequestName} took {ElapsedMs}ms", requestName, elapsedMs);
                        else if (elapsedMs > 1000)
                            _logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms (slow)", requestName, elapsedMs);
                        else
                            _logger.LogDebug("Handled {RequestName} in {ElapsedMs}ms", requestName, elapsedMs);
                    }

                    return response;
                }
                catch (Exception ex)
                {
                    timer.Stop();
                    _logger.LogError(ex,
                        "CRASH: {RequestName} failed after {ElapsedMs}ms. Payload: {@Request}",
                        requestName, timer.ElapsedMilliseconds, SanitizeRequest(request));
                    throw;
                }
            }
        }


        private static object SanitizeRequest(TRequest request)
        {
            var type = request.GetType();
            var properties = type.GetProperties();
            var sanitized = new Dictionary<string, object?>();

            foreach (var prop in properties)
            {
                var val = prop.GetValue(request);
                if (IsSensitive(prop.Name))
                    sanitized[prop.Name] = "***REDACTED***";
                else
                    sanitized[prop.Name] = val;
            }
            return sanitized;
        }

        private static bool IsSensitive(string name)
        {
            var terms = new[] { "password", "token", "secret", "credit", "cvv" };
            return terms.Any(t => name.Contains(t, StringComparison.OrdinalIgnoreCase));
        }

        private static bool ShouldExclude(string name)
        {
            return name.StartsWith("Health") || name.StartsWith("Metrics") || name.StartsWith("Ping");
        }
    }
}