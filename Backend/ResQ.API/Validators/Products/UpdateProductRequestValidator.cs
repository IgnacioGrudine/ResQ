using FluentValidation;
using ResQ.API.DTOs.Products;

namespace ResQ.API.Validators.Products;

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del pack es requerido.")
            .MaximumLength(200);

        RuleFor(x => x.OriginalPrice)
            .GreaterThan(0).WithMessage("El precio original debe ser mayor a cero.");

        RuleFor(x => x.SalePrice)
            .GreaterThan(0).WithMessage("El precio de venta debe ser mayor a cero.")
            .LessThan(x => x.OriginalPrice).WithMessage("El precio de venta debe ser menor al precio original.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("El stock no puede ser negativo.");

        RuleFor(x => x.PickupTimeStart)
            .NotEmpty().WithMessage("El horario de inicio de retiro es requerido.");

        RuleFor(x => x.PickupTimeEnd)
            .NotEmpty().WithMessage("El horario de fin de retiro es requerido.")
            .GreaterThan(x => x.PickupTimeStart).WithMessage("El horario de fin debe ser posterior al de inicio.");
    }
}
