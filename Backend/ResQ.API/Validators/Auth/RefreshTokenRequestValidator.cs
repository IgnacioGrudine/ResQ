using FluentValidation;
using ResQ.API.DTOs.Auth;

namespace ResQ.API.Validators.Auth;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("El refresh token es requerido.");
    }
}
