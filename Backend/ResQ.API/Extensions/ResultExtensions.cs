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
    /// <typeparam name="TIn">The type produced by the service layer.</typeparam>
    /// <typeparam name="TOut">The DTO type returned to the client.</typeparam>
    /// <param name="result">The FluentResults result from the service call.</param>
    /// <param name="map">Projection function that converts <typeparamref name="TIn"/> to <typeparamref name="TOut"/>.</param>
    /// <param name="onSuccess">Optional side-effect to run (e.g. set a cookie) when the result is successful.</param>
    /// <returns>
    /// 200 OK with the mapped value on success, or an appropriate error <see cref="ProblemDetails"/> response on failure.
    /// </returns>
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
    /// <typeparam name="T">The type produced by the service layer and returned to the client.</typeparam>
    /// <param name="result">The FluentResults result from the service call.</param>
    /// <returns>
    /// 200 OK with the result value on success, or an appropriate error <see cref="ProblemDetails"/> response on failure.
    /// </returns>
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
    /// <typeparam name="TIn">The type produced by the service layer.</typeparam>
    /// <typeparam name="TOut">The DTO type returned to the client.</typeparam>
    /// <param name="result">The FluentResults result from the service call.</param>
    /// <param name="map">Projection function that converts <typeparamref name="TIn"/> to <typeparamref name="TOut"/>.</param>
    /// <param name="onSuccess">Optional side-effect to run (e.g. set a cookie) when the result is successful.</param>
    /// <returns>
    /// 201 Created with the mapped value on success, or an appropriate error <see cref="ProblemDetails"/> response on failure.
    /// </returns>
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
    /// <param name="result">The FluentResults result from the service call.</param>
    /// <param name="onSuccess">Optional side-effect to run (e.g. clear a cookie) when the result is successful.</param>
    /// <returns>
    /// 204 No Content on success, or an appropriate error <see cref="ProblemDetails"/> response on failure.
    /// </returns>
    public static IActionResult ToActionResult(this Result result, Action? onSuccess = null)
    {
        if (result.IsFailed)
            return result.ToErrorResponse();

        onSuccess?.Invoke();
        return new NoContentResult();
    }

    // ─── Private helpers (centralise the IError → ActionResult switch) ────────

    /// <summary>
    /// Inspects the first error in <paramref name="result"/> and maps it to the corresponding
    /// HTTP status code and <see cref="ProblemDetails"/> body. Used internally by all public
    /// extension methods to avoid duplicating the error-type switch.
    /// </summary>
    /// <typeparam name="T">The result value type (not used on the error path; required by the return type).</typeparam>
    /// <param name="result">A failed <see cref="ResultBase"/> whose first error determines the HTTP response.</param>
    /// <returns>
    /// An <see cref="ActionResult{T}"/> with the appropriate HTTP status (400, 401, 403, 404, or 409)
    /// and a <see cref="ProblemDetails"/> body describing the error.
    /// </returns>
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

    /// <summary>
    /// Void-result variant of <see cref="ToErrorResponse{T}"/>. Maps the first error in
    /// <paramref name="result"/> to the corresponding HTTP status code and <see cref="ProblemDetails"/> body.
    /// Used by <see cref="ToActionResult(Result, Action)"/> for endpoints that return no body on success.
    /// </summary>
    /// <param name="result">A failed <see cref="ResultBase"/> whose first error determines the HTTP response.</param>
    /// <returns>
    /// An <see cref="IActionResult"/> with the appropriate HTTP status (400, 401, 403, 404, or 409)
    /// and a <see cref="ProblemDetails"/> body describing the error.
    /// </returns>
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
