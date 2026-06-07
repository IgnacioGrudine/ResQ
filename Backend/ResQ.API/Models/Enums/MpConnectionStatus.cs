namespace ResQ.API.Models.Enums;

/// <summary>
/// Represents the state of a merchant's Mercado Pago OAuth connection.
/// Determines whether the merchant is able to receive payments through the platform.
/// </summary>
public enum MpConnectionStatus : byte
{
    /// <summary>
    /// The merchant has not yet completed the Mercado Pago OAuth flow,
    /// or has explicitly revoked the connection. Payments cannot be processed.
    /// </summary>
    Disconnected = 0,

    /// <summary>
    /// The merchant's OAuth tokens are valid and the account is ready to process payments.
    /// </summary>
    Connected = 1,

    /// <summary>
    /// The stored OAuth access token has expired and the automatic refresh job failed.
    /// The merchant must re-authorise via the OAuth flow to restore payment capability.
    /// </summary>
    TokenExpired = 2
}
