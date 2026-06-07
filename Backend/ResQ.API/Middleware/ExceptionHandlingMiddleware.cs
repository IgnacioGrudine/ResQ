using Microsoft.AspNetCore.Mvc;

namespace ResQ.API.Middleware;

/// <summary>
/// Global safety net for unhandled infrastructure exceptions (DB failures, bugs, timeouts).
/// RESPONSIBILITY : Catches anything that escaped the Result Pattern and returns a 500 ProblemDetails.
/// DO NOT USE FOR  : Expected domain errors (404, 400, 401, 409) — those go through Result.Fail.
/// </summary>
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IWebHostEnvironment env)
{
    /// <summary>
    /// Executes the next middleware in the pipeline and intercepts any unhandled exception
    /// that propagates up the call stack. Logs the exception and delegates to
    /// <see cref="HandleExceptionAsync"/> to write a standardised 500 response.
    /// </summary>
    /// <param name="context">The current HTTP request context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Writes a <see cref="ProblemDetails"/> JSON response with HTTP 500 status.
    /// In the Development environment the response includes the exception message and stack trace
    /// to aid debugging; in all other environments a generic message is returned to avoid
    /// leaking implementation details to clients.
    /// </summary>
    /// <param name="context">The current HTTP request context, used to write the response.</param>
    /// <param name="ex">The unhandled exception that was caught by <see cref="InvokeAsync"/>.</param>
    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode  = StatusCodes.Status500InternalServerError;

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title  = "Internal Server Error",
            Detail = env.IsDevelopment()
                ? $"{ex.Message}\n\n{ex.StackTrace}"
                : "An unexpected error occurred. Please try again later.",
            Type   = "https://httpstatuses.com/500"
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}
