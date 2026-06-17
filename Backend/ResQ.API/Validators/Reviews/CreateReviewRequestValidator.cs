using FluentValidation;
using ResQ.API.DTOs.Reviews;

namespace ResQ.API.Validators.Reviews;

/// <summary>
/// FluentValidation validator for <see cref="CreateReviewRequest"/>.
/// Enforces the following business rules:
/// <list type="bullet">
///   <item>Rating must be between 1 and 5 (inclusive).</item>
///   <item>Comment, when provided, must not exceed 1 000 characters.</item>
/// </list>
/// </summary>
public class CreateReviewRequestValidator : AbstractValidator<CreateReviewRequest>
{
    public CreateReviewRequestValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween((byte)1, (byte)5)
            .WithMessage("La calificación debe estar entre 1 y 5 estrellas.");

        RuleFor(x => x.Comment)
            .MaximumLength(1000)
            .WithMessage("El comentario no puede superar los 1 000 caracteres.")
            .When(x => x.Comment is not null);
    }
}
