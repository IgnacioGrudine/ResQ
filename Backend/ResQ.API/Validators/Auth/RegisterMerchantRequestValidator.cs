using FluentValidation;
using ResQ.API.DTOs.Auth;

namespace ResQ.API.Validators.Auth;

public class RegisterMerchantRequestValidator : AbstractValidator<RegisterMerchantRequest>
{
    public RegisterMerchantRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es requerido.")
            .EmailAddress().WithMessage("El email no tiene un formato válido.")
            .MaximumLength(255).WithMessage("El email no puede superar los 255 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es requerida.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .MaximumLength(100).WithMessage("La contraseña no puede superar los 100 caracteres.");

        RuleFor(x => x.BusinessName)
            .NotEmpty().WithMessage("El nombre del comercio es requerido.")
            .MaximumLength(150).WithMessage("El nombre del comercio no puede superar los 150 caracteres.");

        RuleFor(x => x.Cuit)
            .NotEmpty().WithMessage("El CUIT es requerido.")
            .Matches(@"^\d{2}-\d{8}-\d{1}$").WithMessage("El CUIT debe tener el formato XX-XXXXXXXX-X.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("La dirección es requerida.")
            .MaximumLength(255).WithMessage("La dirección no puede superar los 255 caracteres.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m).WithMessage("La latitud debe estar entre -90 y 90.")
            .Must(lat => lat != 0m).WithMessage("La ubicación del comercio es requerida. Seleccioná una dirección válida.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m).WithMessage("La longitud debe estar entre -180 y 180.")
            .Must(lng => lng != 0m).WithMessage("La ubicación del comercio es requerida. Seleccioná una dirección válida.");

        RuleFor(x => x.ContactPhone)
            .NotEmpty().WithMessage("El teléfono de contacto es requerido.")
            .MaximumLength(20).WithMessage("El teléfono no puede superar los 20 caracteres.")
            .Matches(@"^\+?[\d\s\-()+]{7,20}$").WithMessage("El teléfono no tiene un formato válido.");
    }
}
