using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Orders;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Orders;

/// <summary>
/// EF Core implementation of <see cref="IOrderRepository"/>.
/// Provides data access operations for the <see cref="Order"/> entity,
/// extending generic CRUD with queries tailored to consumer and merchant
/// order-management scenarios.
/// </summary>
public class OrderRepository(ResQDbContext db) : GenericRepository<Order>(db), IOrderRepository
{
    /// <summary>
    /// Retrieves all orders placed by the specified consumer, eagerly loading the merchant,
    /// order details, and the product within each detail line. Results are sorted with the
    /// most recently created order first. The query runs as no-tracking.
    /// </summary>
    /// <param name="consumerId">The identifier of the consumer profile whose orders are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A collection of <see cref="Order"/> objects with merchant, order details, and product
    /// data loaded, ordered by creation date descending.
    /// </returns>
    public async Task<IEnumerable<Order>> GetByConsumerIdAsync(int consumerId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Include(o => o.Merchant)
            .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
            .Include(o => o.Review)
            .Where(o => o.ConsumerId == consumerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    /// <summary>
    /// Retrieves all orders directed to the specified merchant, eagerly loading the consumer,
    /// order details, and the product within each detail line. Results are sorted with the
    /// most recently created order first. The query runs as no-tracking.
    /// </summary>
    /// <param name="merchantId">The identifier of the merchant whose orders are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A collection of <see cref="Order"/> objects with consumer, order details, and product
    /// data loaded, ordered by creation date descending.
    /// </returns>
    public async Task<IEnumerable<Order>> GetByMerchantIdAsync(int merchantId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Include(o => o.Consumer)
            .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
            .Where(o => o.MerchantId == merchantId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    /// <summary>
    /// Retrieves the order that matches both the specified merchant and the consumer's
    /// alphanumeric pickup code, eagerly loading the consumer, order details, and products.
    /// Used by the merchant to validate pickup at the point of collection.
    /// </summary>
    /// <param name="merchantId">The identifier of the merchant confirming the pickup.</param>
    /// <param name="pickupCode">The alphanumeric code presented by the consumer.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The matching <see cref="Order"/> with consumer and line-item data loaded,
    /// or <c>null</c> if no order with that code exists for the merchant.
    /// </returns>
    public async Task<Order?> GetByPickupCodeAsync(int merchantId, string pickupCode, CancellationToken ct = default)
        => await _set
            .Include(o => o.Consumer)
            .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.MerchantId == merchantId && o.PickupCode == pickupCode, ct);

    /// <summary>
    /// Retrieves an order by its external reference UUID, which is the correlation key
    /// sent to Mercado Pago as <c>external_reference</c> during checkout creation.
    /// Used by the webhook handler to match an incoming payment notification to its
    /// corresponding internal order.
    /// </summary>
    /// <param name="externalReference">The UUID that was sent to Mercado Pago as <c>external_reference</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The matching <see cref="Order"/>, or <c>null</c> if no order carries that reference.
    /// </returns>
    public async Task<Order?> GetByExternalReferenceAsync(string externalReference, CancellationToken ct = default)
        => await _set
            .FirstOrDefaultAsync(o => o.ExternalReference == externalReference, ct);

    /// <summary>
    /// Retrieves a single order by its identifier, scoped to the specified consumer profile.
    /// Eagerly loads the merchant, order details, and products. The query runs as no-tracking.
    /// Used to let a consumer view the detail of one of their own orders while preventing
    /// access to orders belonging to other consumers.
    /// </summary>
    /// <param name="orderId">The identifier of the order to retrieve.</param>
    /// <param name="consumerProfileId">The identifier of the consumer profile that must own the order.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The <see cref="Order"/> with merchant and line-item data loaded if it belongs to the
    /// specified consumer, or <c>null</c> if no such order exists or ownership does not match.
    /// </returns>
    public async Task<Order?> GetByIdForConsumerAsync(int orderId, int consumerProfileId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Include(o => o.Merchant)
            .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
            .Include(o => o.Review)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.ConsumerId == consumerProfileId, ct);
}
