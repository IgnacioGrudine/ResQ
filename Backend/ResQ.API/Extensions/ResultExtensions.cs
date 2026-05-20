using FluentResults;
using Microsoft.AspNetCore.Mvc;
using ResQ.API.Common.Errors;

namespace ResQ.API.Extensions;

/// <summary>
/// Bridge between the FluentResults world (services) and the ASP.NET Core MVC world (controllers).
/// Translates Result / Result&lt;T&gt; into the appropriate ActionResult + ProblemDetails response.
/// Controllers call one of these methods and return the result — zero if/switch logic allowed there.
/// </summary>
public static class ResultExtensions
{
    // ─── Public methods ───────────────────────────────────────────────────────

    /// <summary>
    /// For GET / PUT endpoints: maps TService → TClient and returns 200 OK on success.
    /// <paramref name="map"/> converts the internal service value to the client DTO.
    /// <paramref name="onSuccess"/> runs side-effects (e.g. setting cookies) before the response is sent.
    /// </summary>
    public static ActionResult<TOut> ToActionResult<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> map,
        Action<TIn>? onSuccess = null)
    {
        if (result.IsFailed)
            return result.ToErrorResponse<TOut>();

        onSuccess?.Invoke(result.Value);
        return new OkObjectResult(map(result.Value));
    }

    /// <summary>
    /// For GET / PUT endpoints that return the same type (no mapping needed). Returns 200 OK on success.
    /// </summary>
    public static ActionResult<T> ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        return result.ToErrorResponse<T>();
    }

    /// <summary>
    /// For POST endpoints that create a resource: maps TService → TClient and returns 201 Created on success.
    /// <paramref name="onSuccess"/> runs side-effects (e.g. setting cookies) before the response is sent.
    /// </summary>
    public static ActionResult<TOut> ToCreatedResult<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> map,
        Action<TIn>? onSuccess = null)
    {
        if (result.IsFailed)
            return result.ToErrorResponse<TOut>();

        onSuccess?.Invoke(result.Value);
        return new ObjectResult(map(result.Value)) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>
    /// For void-success endpoints (DELETE, logout): returns 204 No Content on success.
    /// <paramref name="onSuccess"/> runs side-effects (e.g. clearing cookies) before the response is sent.
    /// </summary>
    public static IActionResult ToActionResult(this Result result, Action? onSuccess = null)
    {
        if (result.IsFailed)
            return result.ToErrorResponse();

        onSuccess?.Invoke();
        return new NoContentResult();
    }

    // ─── Private helpers (centralise the IError → ActionResult switch) ────────

    private static ActionResult<T> ToErrorResponse<T>(this ResultBase result)
    {
        var firstError   = result.Errors.FirstOrDefault();
        var errorMessage = firstError?.Message ?? "An unexpected error occurred.";

        return firstError switch
        {
            NotFoundError => new NotFoundObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title  = "Not Found",
                Detail = errorMessage,
                Type   = "https://httpstatuses.com/404"
            }),

            ValidationError or BadRequestError => new BadRequestObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title  = "Bad Request",
                Detail = errorMessage,
                Type   = "https://httpstatuses.com/400"
            }),

            UnauthorizedError => new UnauthorizedObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title  = "Unauthorized",
                Detail = errorMessage,
                Type   = "https://httpstatuses.com/401"
            }),

            ConflictError => new ConflictObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title  = "Conflict",
                Detail = errorMessage,
                Type   = "https://httpstatuses.com/409"
            }),

            ForbiddenError => new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title  = "Forbidden",
                Detail = errorMessage,
                Type   = "https://httpstatuses.com/403"
            }) { StatusCode = StatusCodes.Status403Forbidden },

            _ => new BadRequestObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title  = "Bad Request",
                Detail = errorMessage,
                Type   = "https://httpstatuses.com/400"
            })
        };
    }

    private static IActionResult ToErrorResponse(this ResultBase result)
    {
        var firstError   = result.Errors.FirstOrDefault();
        var errorMessage = firstError?.Message ?? "An unexpected error occurred.";

        return firstError switch
        {
            NotFoundError => new NotFoundObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title  = "Not Found",
                Detail = errorMessage,
                Type   = "https://httpstatuses.com/404"
            }),

            ValidationError or BadRequestError => new BadRequestObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title  = "Bad Request",
                Detail = errorMessage,
                Type   = "https://httpstatuses.com/400"
            }),

            UnauthorizedError => new UnauthorizedObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title  = "Unauthorized",
                Detail = errorMessage,
                Type   = "https://httpstatuses.com/401"
            }),

            ConflictError => new ConflictObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title  = "Conflict",
                Detail = errorMessage,
                Type   = "https://httpstatuses.com/409"
            }),

            ForbiddenError => new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title  = "Forbidden",
                Detail = errorMessage,
                Type   = "https://httpstatuses.com/403"
            }) { StatusCode = StatusCodes.Status403Forbidden },

            _ => new BadRequestObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title  = "Bad Request",
                Detail = errorMessage,
                Type   = "https://httpstatuses.com/400"
            })
        };
    }
}
