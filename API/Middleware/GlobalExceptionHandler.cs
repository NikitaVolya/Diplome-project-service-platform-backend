using BLL.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace API.Middleware
{
    /// <summary>
    /// Single place that turns exceptions into RFC 7807 problem+json responses.
    /// Services throw AppException subclasses (NotFound, Forbidden, Conflict, ...);
    /// controllers no longer need try/catch blocks.
    /// </summary>
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IHostEnvironment _environment;

        public GlobalExceptionHandler(
            ILogger<GlobalExceptionHandler> logger,
            IHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // The client went away: nothing to answer, nothing to log as an error.
            if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            {
                httpContext.Response.StatusCode = 499;
                return true;
            }

            var (status, title, exposeMessage) = Map(exception);

            if (status >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception while processing {Method} {Path}",
                    httpContext.Request.Method,
                    httpContext.Request.Path);
            }
            else
            {
                _logger.LogWarning(
                    "Request {Method} {Path} failed with {Status}: {Message}",
                    httpContext.Request.Method,
                    httpContext.Request.Path,
                    status,
                    exception.Message);
            }

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Instance = httpContext.Request.Path
            };
            problem.Extensions["traceId"] = httpContext.TraceIdentifier;

            // Details of unexpected / legacy exceptions are shown only in Development.
            if (!exposeMessage && _environment.IsDevelopment())
            {
                problem.Detail = exception.Message;
            }

            httpContext.Response.StatusCode = status;
            await httpContext.Response.WriteAsJsonAsync(
                problem,
                (JsonSerializerOptions?)null,
                "application/problem+json",
                cancellationToken);

            return true;
        }

        private static (int Status, string Title, bool ExposeMessage) Map(Exception exception)
        {
            return exception switch
            {
                // Intentional business errors: the message was written for the client.
                AppException app => (app.StatusCode, app.Message, true),

                // Legacy exceptions still thrown by services that were not migrated yet.
                UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden.", false),
                KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found.", false),
                ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request.", false),

                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", false)
            };
        }
    }
}
