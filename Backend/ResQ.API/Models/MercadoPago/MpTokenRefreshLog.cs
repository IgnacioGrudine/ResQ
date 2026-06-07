using ResQ.API.Models.Auth;
using ResQ.API.Models.Common;

namespace ResQ.API.Models.MercadoPago;

/// <summary>
/// Audit log entry created by the Hangfire daily job that refreshes each merchant's
/// Mercado Pago OAuth access token. Each run of the job produces one log record per
/// merchant processed, regardless of whether the refresh succeeded or failed.
/// Used to diagnose token renewal failures and monitor job health.
/// </summary>
public class MpTokenRefreshLog : BaseEntity
{
    /// <summary>
    /// Foreign key referencing the <see cref="MerchantProfile"/> whose token refresh
    /// this log entry describes.
    /// </summary>
    public int MerchantId { get; set; }

    /// <summary>
    /// Indicates whether the token refresh completed successfully for this merchant.
    /// <c>true</c> means the new access token was obtained and persisted;
    /// <c>false</c> means an error occurred and <see cref="ErrorMessage"/> contains the detail.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Human-readable description of the error that caused the refresh to fail.
    /// Null when <see cref="Success"/> is <c>true</c>.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Navigation property to the merchant whose token refresh this log entry records.
    /// </summary>
    public MerchantProfile Merchant { get; set; } = null!;
}
