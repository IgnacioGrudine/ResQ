using ResQ.API.Models.Catalog;
using ResQ.API.Models.Common;
using ResQ.API.Models.Enums;
using ResQ.API.Models.MercadoPago;
using ResQ.API.Models.Orders;
using ResQ.API.Models.Reviews;

namespace ResQ.API.Models.Auth;

/// <summary>
/// Stores the business information of a merchant — the B2B actor that publishes
/// Surprise Packs, validates consumer pickup codes, and receives payments through
/// their linked Mercado Pago account. One-to-one with <see cref="User"/>.
/// </summary>
public class MerchantProfile : BaseEntity
{
    /// <summary>
    /// Foreign key referencing the <see cref="User"/> account that owns this merchant profile.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// The public-facing name of the merchant's business (e.g. restaurant or bakery name)
    /// displayed to consumers when browsing the platform.
    /// </summary>
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>
    /// Argentine tax identification number (CUIT) of the business, required for fiscal compliance.
    /// </summary>
    public string Cuit { get; set; } = string.Empty;

    /// <summary>
    /// Physical street address of the merchant's location, shown to consumers for pickup reference.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Geographic latitude of the merchant's location, used for proximity-based search on the map.
    /// </summary>
    public decimal Latitude { get; set; }

    /// <summary>
    /// Geographic longitude of the merchant's location, used for proximity-based search on the map.
    /// </summary>
    public decimal Longitude { get; set; }

    /// <summary>
    /// Public contact phone number for the merchant, displayed to consumers on the merchant's profile page.
    /// </summary>
    public string ContactPhone { get; set; } = string.Empty;

    /// <summary>
    /// URL of the merchant's profile photo stored in MinIO object storage. Null if no photo has been uploaded.
    /// </summary>
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Current status of the merchant's Mercado Pago OAuth connection.
    /// Determines whether the merchant can accept payments through the platform.
    /// </summary>
    public MpConnectionStatus MpConnectionStatus { get; set; } = MpConnectionStatus.Disconnected;

    /// <summary>
    /// Navigation property to the <see cref="User"/> account associated with this merchant profile.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// The food categories (e.g. bakery, sushi, coffee) that describe the type of Surprise Packs
    /// this merchant offers. Populated via the many-to-many join table <see cref="MerchantCategory"/>.
    /// </summary>
    public ICollection<MerchantCategory> MerchantCategories { get; set; } = [];

    /// <summary>
    /// All Surprise Pack products published by this merchant on the platform.
    /// </summary>
    public ICollection<Product> Products { get; set; } = [];

    /// <summary>
    /// All orders placed by consumers at this merchant's location.
    /// </summary>
    public ICollection<Order> Orders { get; set; } = [];

    /// <summary>
    /// All consumer reviews submitted for this merchant after a completed order.
    /// </summary>
    public ICollection<Review> Reviews { get; set; } = [];

    /// <summary>
    /// The merchant's Mercado Pago OAuth credential record, which holds AES-256-CBC encrypted
    /// access and refresh tokens. Null if the merchant has not yet connected their MP account.
    /// </summary>
    public MerchantMpCredential? MpCredential { get; set; }

    /// <summary>
    /// Audit log entries produced by the Hangfire daily job that refreshes this merchant's
    /// Mercado Pago OAuth tokens. Useful for diagnosing renewal failures.
    /// </summary>
    public ICollection<MpTokenRefreshLog> MpTokenRefreshLogs { get; set; } = [];
}
