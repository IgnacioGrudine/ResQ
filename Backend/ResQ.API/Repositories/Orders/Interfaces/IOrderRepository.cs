using ResQ.API.Models.Orders;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Orders;

public interface IOrderRepository : IGenericRepository<Order>
{
    /// <summary>
    /// Returns all orders placed by the given consumer, including their associated
    /// order details and product information.
    /// </summary>
    /// <param name="consumerId">The identifier of the consumer profile whose orders are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A collection of <see cref="Order"/> entities belonging to the consumer, ordered
    /// from most recent to oldest; an empty collection if the consumer has no orders.
    /// </returns>
    Task<IEnumerable<Order>> GetByConsumerIdAsync(int consumerId, CancellationToken ct = default);

    /// <summary>
    /// Returns all orders associated with the given merchant, including consumer
    /// and product information needed to render the merchant's order dashboard.
    /// </summary>
    /// <param name="merchantId">The identifier of the merchant profile whose orders are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A collection of <see cref="Order"/> entities associated with the merchant;
    /// an empty collection if the merchant has received no orders.
    /// </returns>
    Task<IEnumerable<Order>> GetByMerchantIdAsync(int merchantId, CancellationToken ct = default);

    /// <summary>Tracked lookup of a single order by merchant + pickup code, for status mutation.</summary>
    /// <remarks>
    /// Returns the order in a tracked state so that EF Core will detect and persist
    /// status changes when <c>SaveChangesAsync</c> is called. Used exclusively by the
    /// pickup-confirmation flow where the order status must be mutated.
    /// </remarks>
    /// <param name="merchantId">The identifier of the merchant confirming the pickup.</param>
    /// <param name="pickupCode">The alphanumeric pickup code presented by the consumer.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The tracked <see cref="Order"/> if a pending paid order with the given pickup code
    /// exists for the merchant, or <c>null</c> otherwise.
    /// </returns>
    Task<Order?> GetByPickupCodeAsync(int merchantId, string pickupCode, CancellationToken ct = default);

    /// <summary>Tracked lookup by ExternalReference UUID — used by the webhook handler.</summary>
    /// <remarks>
    /// Returns the order in a tracked state so that the webhook processor can update
    /// its payment status and persist the change. The <c>ExternalReference</c> is a UUID
    /// generated at order creation time and sent to Mercado Pago as the correlation key.
    /// </remarks>
    /// <param name="externalReference">The UUID string that was set as <c>external_reference</c> in the Mercado Pago payment preference.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The tracked <see cref="Order"/> matching the external reference, or <c>null</c>
    /// if not found.
    /// </returns>
    Task<Order?> GetByExternalReferenceAsync(string externalReference, CancellationToken ct = default);

    /// <summary>Read-only lookup of a single consumer order with full details — used for order detail / polling.</summary>
    /// <remarks>
    /// The query uses <c>AsNoTracking</c> for performance since no state mutation is
    /// expected. Validates that the order belongs to the specified consumer to prevent
    /// cross-consumer data leaks.
    /// </remarks>
    /// <param name="orderId">The identifier of the order to retrieve.</param>
    /// <param name="consumerProfileId">The identifier of the consumer profile that must own the order.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The <see cref="Order"/> with full navigation properties populated if it exists and
    /// belongs to the consumer, or <c>null</c> otherwise.
    /// </returns>
    Task<Order?> GetByIdForConsumerAsync(int orderId, int consumerProfileId, CancellationToken ct = default);

    /// <summary>Tracked lookup of a single consumer order — used by the self-service cancellation flow.</summary>
    /// <remarks>
    /// Returns the order in a tracked state, with the product of each order detail loaded,
    /// so the caller can both mutate <c>OrderStatus</c> and restore <c>Product.StockQuantity</c>
    /// within the same <c>SaveChangesAsync</c> call.
    /// </remarks>
    /// <param name="orderId">The identifier of the order to retrieve.</param>
    /// <param name="consumerProfileId">The identifier of the consumer profile that must own the order.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The tracked <see cref="Order"/> with merchant and product data loaded if it belongs to
    /// the consumer, or <c>null</c> otherwise.
    /// </returns>
    Task<Order?> GetByIdForConsumerTrackedAsync(int orderId, int consumerProfileId, CancellationToken ct = default);
}
