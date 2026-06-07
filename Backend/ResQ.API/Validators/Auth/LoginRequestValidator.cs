using FluentValidation;
using ResQ.API.DTOs.Auth;

namespace ResQ.API.Validators.Auth;

/// <summary>
/// FluentValidation validator for <see cref="LoginRequest"/>.
/// Enforces that the email field is present and well-formed, and that the password
/// field is not empty before the authentication attempt is processed.
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es requerido.")
            .EmailAddress().WithMessage("El email no tiene un formato válido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es requerida.");
    }
}
