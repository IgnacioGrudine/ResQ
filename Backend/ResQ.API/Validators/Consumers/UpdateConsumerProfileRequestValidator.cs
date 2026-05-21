using FluentValidation;
using ResQ.API.DTOs.Consumers;

namespace ResQ.API.Validators.Consumers;

public class UpdateConsumerProfileRequestValidator : AbstractValidator<UpdateConsumerProfileRequest>
{
    public UpdateConsumerProfileRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es requerido.")
            .MaximumLength(100);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .When(x => x.PhoneNumber is not null);
    }
}
