using FluentValidation;
using ResQ.API.DTOs.Auth;

namespace ResQ.API.Validators.Auth;

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
