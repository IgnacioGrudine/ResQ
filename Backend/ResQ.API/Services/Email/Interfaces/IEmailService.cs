namespace ResQ.API.Services.Email;

public interface IEmailService
{
    Task SendReviewRequestAsync(
        string toEmail,
        string consumerName,
        string merchantName,
        int orderId,
        CancellationToken ct = default);

    /// <summary>
    /// Sends the password reset link to a user who requested it via the forgot-password flow.
    /// </summary>
    /// <param name="toEmail">The recipient's email address.</param>
    /// <param name="userName">The recipient's first name, used to personalise the greeting.</param>
    /// <param name="resetUrl">The full frontend URL (including the token query parameter) the user must click.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendPasswordResetAsync(
        string toEmail,
        string userName,
        string resetUrl,
        CancellationToken ct = default);

    /// <summary>
    /// Sends an informational email to a Google-only account (no password set) that
    /// requested a password reset, explaining that they must sign in with Google instead.
    /// </summary>
    /// <param name="toEmail">The recipient's email address.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendGoogleOnlyAccountNoticeAsync(
        string toEmail,
        CancellationToken ct = default);
}
