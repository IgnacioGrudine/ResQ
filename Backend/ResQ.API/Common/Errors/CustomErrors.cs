using FluentResults;

namespace ResQ.API.Common.Errors;

/// <summary>Requested resource does not exist.</summary>
public class NotFoundError : Error
{
    public NotFoundError(string message) : base(message) { }
}

/// <summary>Request payload failed validation rules.</summary>
public class ValidationError : Error
{
    public ValidationError(string message) : base(message) { }
}

/// <summary>Request is syntactically valid but semantically incorrect.</summary>
public class BadRequestError : Error
{
    public BadRequestError(string message) : base(message) { }
}

/// <summary>Caller is not authenticated or credentials are invalid.</summary>
public class UnauthorizedError : Error
{
    public UnauthorizedError(string message) : base(message) { }
}

/// <summary>The resource already exists or the operation conflicts with current state.</summary>
public class ConflictError : Error
{
    public ConflictError(string message) : base(message) { }
}

/// <summary>Caller is authenticated but lacks permission to perform the action.</summary>
public class ForbiddenError : Error
{
    public ForbiddenError(string message) : base(message) { }
}
