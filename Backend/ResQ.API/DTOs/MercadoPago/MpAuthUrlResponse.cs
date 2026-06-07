namespace ResQ.API.DTOs.MercadoPago;

/// <summary>
/// Response returned when a merchant initiates the Mercado Pago OAuth connection flow,
/// containing the URL to which the merchant must be redirected to grant authorization.
/// </summary>
public class MpAuthUrlResponse
{
    /// <summary>
    /// The Mercado Pago OAuth authorization URL. The merchant must navigate to this URL
    /// to grant ResQ access to their Mercado Pago account.
    /// </summary>
    public string AuthUrl { get; set; } = string.Empty;
}
