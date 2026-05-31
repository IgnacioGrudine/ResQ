using FluentValidation;
using ResQ.API.DTOs.Merchants;

namespace ResQ.API.Validators.Merchants;

public class UpdateMerchantProfileRequestValidator : AbstractValidator<UpdateMerchantProfileRequest>
{
    public UpdateMerchantProfileRequestValidator()
    {
        RuleFor(x => x.BusinessName)
            .NotEmpty().WithMessage("El nombre del comercio es requerido.")
            .MaximumLength(200);

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("La dirección es requerida.")
            .MaximumLength(500);

        RuleFor(x => x.ContactPhone)
            .NotEmpty().WithMessage("El teléfono de contacto es requerido.")
            .MaximumLength(20);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m).WithMessage("Latitud inválida.")
            .Must(lat => lat != 0m).WithMessage("La ubicación del comercio es requerida. Seleccioná una dirección válida.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m).WithMessage("Longitud inválida.")
            .Must(lng => lng != 0m).WithMessage("La ubicación del comercio es requerida. Seleccioná una dirección válida.");
    }
}
