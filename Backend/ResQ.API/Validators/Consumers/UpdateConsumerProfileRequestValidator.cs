using FluentValidation;
using ResQ.API.DTOs.Consumers;

namespace ResQ.API.Validators.Consumers;

/// <summary>
/// FluentValidation validator for <see cref="UpdateConsumerProfileRequest"/>.
/// Enforces the following business rules when a consumer updates their profile:
/// <list type="bullet">
///   <item>First name is required and capped at 100 characters.</item>
///   <item>Last name is required and capped at 100 characters.</item>
///   <item>Phone number, when provided (non-null), must be at most 20 characters.</item>
/// </list>
/// </summary>
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
