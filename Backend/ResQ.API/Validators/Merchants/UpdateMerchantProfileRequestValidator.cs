using FluentValidation;
using ResQ.API.DTOs.Merchants;

namespace ResQ.API.Validators.Merchants;

/// <summary>
/// FluentValidation validator for <see cref="UpdateMerchantProfileRequest"/>.
/// Enforces the following business rules when a merchant updates their profile:
/// <list type="bullet">
///   <item>Business name is required and capped at 200 characters.</item>
///   <item>Address is required and capped at 500 characters.</item>
///   <item>Contact phone is required and capped at 20 characters.</item>
///   <item>Latitude must be between -90 and 90 and must not be exactly 0
///         (a zero value indicates the user did not select a valid map location).</item>
///   <item>Longitude must be between -180 and 180 and must not be exactly 0.</item>
/// </list>
/// </summary>
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
