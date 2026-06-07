using FluentValidation;
using ResQ.API.DTOs.Auth;

namespace ResQ.API.Validators.Auth;

/// <summary>
/// FluentValidation validator for <see cref="RefreshTokenRequest"/>.
/// Enforces that the refresh token field is not empty before the token exchange
/// is attempted, preventing unnecessary database lookups with blank values.
/// </summary>
public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("El refresh token es requerido.");
    }
}
