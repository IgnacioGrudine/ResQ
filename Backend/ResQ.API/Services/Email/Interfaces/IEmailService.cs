namespace ResQ.API.Services.Email;

public interface IEmailService
{
    Task SendReviewRequestAsync(
        string toEmail,
        string consumerName,
        string merchantName,
        int orderId,
        CancellationToken ct = default);
}
