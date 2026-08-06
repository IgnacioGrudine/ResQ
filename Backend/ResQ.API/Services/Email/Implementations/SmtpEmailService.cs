using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using ResQ.API.Models.Settings;

namespace ResQ.API.Services.Email;

public class SmtpEmailService(
    IOptions<SmtpSettings> smtpOptions,
    IOptions<MpSettings> mpOptions,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly SmtpSettings _smtp   = smtpOptions.Value;
    private readonly string       _appUrl = mpOptions.Value.FrontendBaseUrl.TrimEnd('/');

    public async Task SendReviewRequestAsync(
        string toEmail,
        string consumerName,
        string merchantName,
        int orderId,
        CancellationToken ct = default)
    {
        var reviewUrl = $"{_appUrl}/mis-pedidos/{orderId}/reseña";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = $"¿Cómo estuvo tu experiencia en {merchantName}? 🌿";
        message.Body = new TextPart("html") { Text = BuildHtml(consumerName, merchantName, reviewUrl) };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(_smtp.Username, _smtp.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            logger.LogInformation("[Email] Review request sent to {Email} for order #{OrderId}", toEmail, orderId);
        }
        catch (Exception ex)
        {
            // Never block pickup confirmation — log and continue
            logger.LogError(ex, "[Email] Failed to send review request to {Email} for order #{OrderId}", toEmail, orderId);
        }
    }

    public async Task SendPasswordResetAsync(
        string toEmail,
        string userName,
        string resetUrl,
        CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Recuperá tu contraseña de ResQ 🔐";
        message.Body = new TextPart("html") { Text = BuildPasswordResetHtml(userName, resetUrl) };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(_smtp.Username, _smtp.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            logger.LogInformation("[Email] Password reset link sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            // The caller has already committed the token to the database — a delivery
            // failure here must not surface to the client (would leak account existence).
            logger.LogError(ex, "[Email] Failed to send password reset link to {Email}", toEmail);
        }
    }

    public async Task SendGoogleOnlyAccountNoticeAsync(
        string toEmail,
        CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Tu cuenta de ResQ usa Google para iniciar sesión";
        message.Body = new TextPart("html") { Text = BuildGoogleOnlyHtml() };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(_smtp.Username, _smtp.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            logger.LogInformation("[Email] Google-only account notice sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Email] Failed to send Google-only account notice to {Email}", toEmail);
        }
    }

    private static string BuildPasswordResetHtml(string userName, string resetUrl) => $$"""
        <!DOCTYPE html>
        <html lang="es">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <style>
            body { margin: 0; padding: 0; background-color: #f4f4f5; font-family: Arial, sans-serif; }
            .wrapper { max-width: 600px; margin: 40px auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 24px rgba(0,0,0,0.08); }
            .header { background: linear-gradient(135deg, #1b4332 0%, #2d6a4f 100%); padding: 36px 32px; text-align: center; }
            .header-logo { color: #ffffff; font-size: 32px; font-weight: bold; margin: 0; letter-spacing: -0.5px; }
            .header-tagline { color: #74c69d; font-size: 13px; margin: 4px 0 0; }
            .body { padding: 40px 32px; }
            .greeting { color: #1b4332; font-size: 22px; font-weight: bold; margin: 0 0 16px; }
            .text { color: #4a5568; font-size: 15px; line-height: 1.7; margin: 0 0 20px; }
            .cta { text-align: center; margin: 32px 0; }
            .btn { display: inline-block; background-color: #2d6a4f; color: #ffffff !important; text-decoration: none; padding: 16px 44px; border-radius: 10px; font-size: 16px; font-weight: bold; }
            .link-fallback { color: #9ca3af; font-size: 12px; text-align: center; margin-top: 20px; word-break: break-all; }
            .divider { border: none; border-top: 1px solid #e5e7eb; margin: 32px 0; }
            .footer { background: #f9fafb; padding: 24px 32px; text-align: center; color: #9ca3af; font-size: 12px; line-height: 1.6; }
          </style>
        </head>
        <body>
          <div class="wrapper">
            <div class="header">
              <p class="header-logo">🌿 ResQ</p>
              <p class="header-tagline">Rescate de alimentos</p>
            </div>
            <div class="body">
              <h1 class="greeting">Hola, {{userName}}</h1>
              <p class="text">
                Recibimos una solicitud para restablecer la contraseña de tu cuenta ResQ.
                Hacé clic en el botón para elegir una nueva contraseña. Este enlace es válido
                por <strong>1 hora</strong> y solo puede usarse una vez.
              </p>
              <div class="cta">
                <a href="{{resetUrl}}" class="btn">Restablecer contraseña</a>
              </div>
              <p class="link-fallback">
                Si el botón no funciona, copiá este enlace:<br>
                <a href="{{resetUrl}}" style="color: #6b7280;">{{resetUrl}}</a>
              </p>
              <hr class="divider">
              <p class="text" style="font-size:13px; color:#9ca3af; margin:0;">
                Si no solicitaste este cambio, podés ignorar este correo — tu contraseña actual
                seguirá funcionando con normalidad.
              </p>
            </div>
            <div class="footer">
              <p><strong>ResQ</strong> — Rescate de alimentos</p>
              <p>Este correo fue enviado porque se solicitó restablecer la contraseña de esta cuenta.</p>
            </div>
          </div>
        </body>
        </html>
        """;

    private static string BuildGoogleOnlyHtml() => $$"""
        <!DOCTYPE html>
        <html lang="es">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <style>
            body { margin: 0; padding: 0; background-color: #f4f4f5; font-family: Arial, sans-serif; }
            .wrapper { max-width: 600px; margin: 40px auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 24px rgba(0,0,0,0.08); }
            .header { background: linear-gradient(135deg, #1b4332 0%, #2d6a4f 100%); padding: 36px 32px; text-align: center; }
            .header-logo { color: #ffffff; font-size: 32px; font-weight: bold; margin: 0; letter-spacing: -0.5px; }
            .header-tagline { color: #74c69d; font-size: 13px; margin: 4px 0 0; }
            .body { padding: 40px 32px; }
            .greeting { color: #1b4332; font-size: 22px; font-weight: bold; margin: 0 0 16px; }
            .text { color: #4a5568; font-size: 15px; line-height: 1.7; margin: 0 0 20px; }
            .footer { background: #f9fafb; padding: 24px 32px; text-align: center; color: #9ca3af; font-size: 12px; line-height: 1.6; }
          </style>
        </head>
        <body>
          <div class="wrapper">
            <div class="header">
              <p class="header-logo">🌿 ResQ</p>
              <p class="header-tagline">Rescate de alimentos</p>
            </div>
            <div class="body">
              <h1 class="greeting">Tu cuenta usa Google</h1>
              <p class="text">
                Recibimos una solicitud para restablecer la contraseña de esta cuenta, pero
                notamos que te registraste con <strong>Google Sign-In</strong> — no tenés una
                contraseña de ResQ que restablecer.
              </p>
              <p class="text">
                Para iniciar sesión, usá el botón "Continuar con Google" en la pantalla de login.
              </p>
              <p class="text" style="font-size:13px; color:#9ca3af; margin-top:24px;">
                Si no solicitaste esto, podés ignorar este correo.
              </p>
            </div>
            <div class="footer">
              <p><strong>ResQ</strong> — Rescate de alimentos</p>
            </div>
          </div>
        </body>
        </html>
        """;

    private static string BuildHtml(string consumerName, string merchantName, string reviewUrl) => $$"""
        <!DOCTYPE html>
        <html lang="es">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <style>
            body { margin: 0; padding: 0; background-color: #f4f4f5; font-family: Arial, sans-serif; }
            .wrapper { max-width: 600px; margin: 40px auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 24px rgba(0,0,0,0.08); }
            .header { background: linear-gradient(135deg, #1b4332 0%, #2d6a4f 100%); padding: 36px 32px; text-align: center; }
            .header-logo { color: #ffffff; font-size: 32px; font-weight: bold; margin: 0; letter-spacing: -0.5px; }
            .header-tagline { color: #74c69d; font-size: 13px; margin: 4px 0 0; }
            .body { padding: 40px 32px; }
            .greeting { color: #1b4332; font-size: 22px; font-weight: bold; margin: 0 0 16px; }
            .text { color: #4a5568; font-size: 15px; line-height: 1.7; margin: 0 0 20px; }
            .merchant { color: #2d6a4f; font-weight: bold; }
            .stars { font-size: 28px; text-align: center; margin: 8px 0 24px; letter-spacing: 4px; }
            .cta { text-align: center; margin: 32px 0; }
            .btn { display: inline-block; background-color: #2d6a4f; color: #ffffff !important; text-decoration: none; padding: 16px 44px; border-radius: 10px; font-size: 16px; font-weight: bold; }
            .link-fallback { color: #9ca3af; font-size: 12px; text-align: center; margin-top: 20px; word-break: break-all; }
            .divider { border: none; border-top: 1px solid #e5e7eb; margin: 32px 0; }
            .footer { background: #f9fafb; padding: 24px 32px; text-align: center; color: #9ca3af; font-size: 12px; line-height: 1.6; }
          </style>
        </head>
        <body>
          <div class="wrapper">
            <div class="header">
              <p class="header-logo">🌿 ResQ</p>
              <p class="header-tagline">Rescate de alimentos</p>
            </div>
            <div class="body">
              <h1 class="greeting">¡Gracias por tu compra, {{consumerName}}!</h1>
              <p class="text">
                Esperamos que hayas disfrutado tu pack sorpresa de
                <span class="merchant">{{merchantName}}</span>.
                Tu opinión ayuda a otros usuarios a descubrir los mejores comercios de la ciudad y motiva
                a los comercios a seguir rescatando alimentos. 🙌
              </p>
              <div class="stars">⭐⭐⭐⭐⭐</div>
              <p class="text">¿Podés contarnos cómo fue tu experiencia? Solo te lleva un minuto.</p>
              <div class="cta">
                <a href="{{reviewUrl}}" class="btn">Dejar mi reseña</a>
              </div>
              <p class="link-fallback">
                Si el botón no funciona, copiá este enlace:<br>
                <a href="{{reviewUrl}}" style="color: #6b7280;">{{reviewUrl}}</a>
              </p>
              <hr class="divider">
              <p class="text" style="font-size:13px; color:#9ca3af; margin:0;">
                Si no realizaste esta compra podés ignorar este correo.
              </p>
            </div>
            <div class="footer">
              <p><strong>ResQ</strong> — Rescate de alimentos</p>
              <p>Este correo fue enviado porque realizaste una compra en nuestra plataforma.</p>
            </div>
          </div>
        </body>
        </html>
        """;
}
