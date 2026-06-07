namespace ResQ.API.Models.Enums;

/// <summary>
/// Defines the access roles available on the ResQ platform.
/// A user may hold multiple roles simultaneously.
/// </summary>
public enum Role : byte
{
    /// <summary>
    /// A registered consumer who browses merchant listings, places orders,
    /// pays via Mercado Pago, and picks up Surprise Packs using a 6-character code.
    /// </summary>
    Consumer = 1,

    /// <summary>
    /// A registered merchant (business) that publishes Surprise Packs, manages stock,
    /// validates consumer pickup codes, and connects a Mercado Pago account to receive payments.
    /// </summary>
    Merchant = 2,

    /// <summary>
    /// A platform administrator with full access to manage users, monitor operations,
    /// and receive the platform commission via Mercado Pago's marketplace fee.
    /// </summary>
    Admin = 3
}
