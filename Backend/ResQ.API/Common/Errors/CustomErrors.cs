using FluentResults;

namespace ResQ.API.Common.Errors;

/// <summary>
/// Represents a FluentResults error for a resource that does not exist in the system.
/// Mapped to HTTP 404 Not Found by <c>ResultExtensions</c>.
/// </summary>
public class NotFoundError : Error
{
    /// <summary>
    /// Initialises a new <see cref="NotFoundError"/> with a message describing which resource was not found.
    /// </summary>
    /// <param name="message">Human-readable description of the missing resource.</param>
    public NotFoundError(string message) : base(message) { }
}

/// <summary>
/// Represents a FluentResults error for a request payload that failed validation rules.
/// Mapped to HTTP 400 Bad Request by <c>ResultExtensions</c>.
/// </summary>
public class ValidationError : Error
{
    /// <summary>
    /// Initialises a new <see cref="ValidationError"/> with a message describing which validation rule was violated.
    /// </summary>
    /// <param name="message">Human-readable description of the validation failure.</param>
    public ValidationError(string message) : base(message) { }
}

/// <summary>
/// Represents a FluentResults error for a request that is syntactically valid but semantically incorrect.
/// Use when input data is well-formed but violates a domain rule (e.g. picking up an already-completed order).
/// Mapped to HTTP 400 Bad Request by <c>ResultExtensions</c>.
/// </summary>
public class BadRequestError : Error
{
    /// <summary>
    /// Initialises a new <see cref="BadRequestError"/> with a message describing the semantic problem.
    /// </summary>
    /// <param name="message">Human-readable description of why the request cannot be fulfilled.</param>
    public BadRequestError(string message) : base(message) { }
}

/// <summary>
/// Represents a FluentResults error for a caller that is not authenticated or whose credentials are invalid.
/// Mapped to HTTP 401 Unauthorized by <c>ResultExtensions</c>.
/// </summary>
public class UnauthorizedError : Error
{
    /// <summary>
    /// Initialises a new <see cref="UnauthorizedError"/> with a message describing the authentication failure.
    /// </summary>
    /// <param name="message">Human-readable description of why authentication failed.</param>
    public UnauthorizedError(string message) : base(message) { }
}

/// <summary>
/// Represents a FluentResults error for an operation that conflicts with the current state of a resource
/// (e.g. attempting to register with an email address that is already in use).
/// Mapped to HTTP 409 Conflict by <c>ResultExtensions</c>.
/// </summary>
public class ConflictError : Error
{
    /// <summary>
    /// Initialises a new <see cref="ConflictError"/> with a message describing the conflicting state.
    /// </summary>
    /// <param name="message">Human-readable description of the conflict.</param>
    public ConflictError(string message) : base(message) { }
}

/// <summary>
/// Represents a FluentResults error for a caller who is authenticated but lacks the necessary
/// permission to perform the requested action (e.g. a consumer attempting a merchant-only operation).
/// Mapped to HTTP 403 Forbidden by <c>ResultExtensions</c>.
/// </summary>
public class ForbiddenError : Error
{
    /// <summary>
    /// Initialises a new <see cref="ForbiddenError"/> with a message describing the missing permission.
    /// </summary>
    /// <param name="message">Human-readable description of why the action is forbidden.</param>
    public ForbiddenError(string message) : base(message) { }
}
