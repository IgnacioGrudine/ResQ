using ResQ.API.Models.Catalog;
using ResQ.API.Models.Common;

namespace ResQ.API.Models.Orders;

/// <summary>
/// Represents a single line item within an <see cref="Order"/>, recording which product
/// was purchased, how many units, and the price at the time of purchase.
/// The price is captured as a snapshot (<see cref="UnitPrice"/>) so that subsequent
/// changes to the product's sale price do not affect historical order records.
/// </summary>
public class OrderDetail : BaseEntity
{
    /// <summary>
    /// Foreign key referencing the <see cref="Order"/> that contains this line item.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Foreign key referencing the <see cref="Product"/> that was purchased.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Number of units of the product included in this line item.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// The sale price per unit of the product at the moment the order was placed, in ARS.
    /// Stored as a snapshot to preserve price history independently of future product price changes.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Navigation property to the parent <see cref="Order"/> that contains this line item.
    /// </summary>
    public Order Order { get; set; } = null!;

    /// <summary>
    /// Navigation property to the <see cref="Product"/> purchased in this line item.
    /// </summary>
    public Product Product { get; set; } = null!;
}
