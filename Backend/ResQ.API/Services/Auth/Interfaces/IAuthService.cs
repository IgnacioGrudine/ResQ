using FluentResults;
using ResQ.API.DTOs.Auth;

namespace ResQ.API.Services.Auth;

public interface IAuthService
{
    /// <summary>
    /// Registers a new consumer account, creates the associated consumer profile,
    /// and returns a pair of access and refresh tokens ready for immediate use.
    /// </summary>
    /// <param name="request">Registration data including email, password, first name, and last name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing an <see cref="AuthResponse"/> with the
    /// access token, refresh token, and basic profile information; or a failed result with
    /// validation or conflict errors.
    /// </returns>
    Task<Result<AuthResponse>> RegisterConsumerAsync(RegisterConsumerRequest request, CancellationToken ct = default);

    /// <summary>
    /// Registers a new merchant account, creates the associated merchant profile,
    /// and returns a pair of access and refresh tokens ready for immediate use.
    /// </summary>
    /// <param name="request">Registration data including email, password, business name, CUIT, and address.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing an <see cref="AuthResponse"/> with the
    /// access token, refresh token, and basic profile information; or a failed result with
    /// validation or conflict errors.
    /// </returns>
    Task<Result<AuthResponse>> RegisterMerchantAsync(RegisterMerchantRequest request, CancellationToken ct = default);

    /// <summary>
    /// Authenticates a user with email and password and issues a new pair of
    /// access and refresh tokens.
    /// </summary>
    /// <param name="request">Login credentials containing the user's email and plain-text password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing an <see cref="AuthResponse"/> on valid
    /// credentials; or a failed result if the email does not exist or the password is incorrect.
    /// </returns>
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>
    /// Validates the provided refresh token, rotates it, and issues a new pair of
    /// access and refresh tokens.
    /// </summary>
    /// <param name="refreshToken">The opaque refresh token string previously issued by the server.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing a new <see cref="AuthResponse"/>; or a
    /// failed result if the token is not found, has expired, or has already been revoked.
    /// </returns>
    Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Revokes the provided refresh token so it can no longer be used to obtain new tokens.
    /// </summary>
    /// <param name="refreshToken">The opaque refresh token string to invalidate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result"/> if the token was found and revoked; or a failed result
    /// if the token does not exist.
    /// </returns>
    Task<Result> LogoutAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Starts the password-reset flow for the given email. Always succeeds regardless of
    /// whether the email is registered, to avoid leaking account existence to the caller.
    /// </summary>
    /// <param name="email">The email address supplied on the "forgot password" form.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Always a successful <see cref="Result"/>.</returns>
    Task<Result> ForgotPasswordAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Completes the password-reset flow: validates the token, sets the new password,
    /// and revokes all of the user's active refresh tokens.
    /// </summary>
    /// <param name="request">Contains the opaque reset token and the new plaintext password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result"/> on success; a failed result with a
    /// <c>BadRequestError</c> if the token is invalid, expired, or already used.
    /// </returns>
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
}
