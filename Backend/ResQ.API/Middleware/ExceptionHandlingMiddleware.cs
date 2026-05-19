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
