using FluentValidation;
using ResQ.API.DTOs.Auth;

namespace ResQ.API.Validators.Auth;

/// <summary>
/// FluentValidation validator for <see cref="RegisterConsumerRequest"/>.
/// Enforces the following business rules for consumer registration:
/// <list type="bullet">
///   <item>Email must be present, valid, and at most 255 characters.</item>
///   <item>Password must be between 8 and 100 characters.</item>
///   <item>First name and last name are required and capped at 100 characters each.</item>
///   <item>Phone number, when provided, must be at most 20 characters and match
///         a permissive international phone format (<c>^\+?[\d\s\-()+]{7,20}$</c>).</item>
/// </list>
/// </summary>
public class RegisterConsumerRequestValidator : AbstractValidator<RegisterConsumerRequest>
{
    public RegisterConsumerRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es requerido.")
            .EmailAddress().WithMessage("El email no tiene un formato válido.")
            .MaximumLength(255).WithMessage("El email no puede superar los 255 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es requerida.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .MaximumLength(100).WithMessage("La contraseña no puede superar los 100 caracteres.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es requerido.")
            .MaximumLength(100).WithMessage("El apellido no puede superar los 100 caracteres.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("El teléfono no puede superar los 20 caracteres.")
            .Matches(@"^\+?[\d\s\-()+]{7,20}$").WithMessage("El teléfono no tiene un formato válido.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}
