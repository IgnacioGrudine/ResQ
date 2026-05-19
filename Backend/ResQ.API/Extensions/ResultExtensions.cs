using FluentResults;
using Microsoft.AspNetCore.Mvc;
using ResQ.API.Common.Errors;

namespace ResQ.API.Extensions;

/// <summary>
/// Bridge between the FluentResults world (services) and the ASP.NET Core MVC world (controllers).
/// Translates Result / Result&lt;T&gt; into the appropriate ActionResult + ProblemDetails response.
/// </summary>
public static class ResultExtensions
{
    // ─── Public methods ───────────────────────────────────────────────────────

    /// <summary>
    /// For GET / PUT endpoints that return a typed body on success (200 OK).
    /// </summary>
    public static ActionResult<T> ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        return result.ToErrorResponse<T>();
    }

    /// <summary>
    /// For DELETE (and any void-success) endpoints that return 204 No Content on success.
    /// </summary>
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new NoContentResult();

        return result.ToErrorResponse();
    }

    /// <summary>
    /// For POST endpoints that return 201 Created + Location header on success.
    /// <paramref name="routeValuesSelector"/> is a deferred lambda so .Value is only
    /// accessed after the IsSuccess guard — avoids InvalidOperationException on failure.
    /// </summary>
    public static ActionResult<T> ToCreatedResult<T>(
        this Result<T> result,
        string actionName,
        Func<T, object> routeValuesSelector)
    {
        if (result.IsFailed)
            return result.ToErrorResponse<T>();

        var routeValues = routeValuesSelector(result.Value);
        return new CreatedAtActionResult(actionName, null, routeValues, result.Value);
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
