using FluentResults;
using ResQ.API.DTOs.Orders;

namespace ResQ.API.Services.Orders;

public interface IOrderService
{
    /// <summary>
    /// Creates a new order for the authenticated consumer, initiates the Mercado Pago
    /// payment preference, and returns the payment URL and internal order reference.
    /// </summary>
    /// <remarks>
    /// The order is created in a <c>Pending</c> status. A unique UUID is generated and
    /// stored as <c>ExternalReference</c> to correlate the eventual Mercado Pago webhook
    /// with this order.
    /// </remarks>
    /// <param name="consumerProfileId">The identifier of the consumer profile placing the order.</param>
    /// <param name="request">DTO containing the product identifier and quantity being ordered.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing an <see cref="OrderCreatedResponse"/>
    /// with the internal order ID and the Mercado Pago checkout URL; or a failed result if
    /// the product is unavailable, stock is insufficient, or payment preference creation fails.
    /// </returns>
    Task<Result<OrderCreatedResponse>> CreateOrderAsync(int consumerProfileId, CreateOrderRequest request, CancellationToken ct = default);

    /// <summary>
    /// Returns the full order history for the authenticated consumer.
    /// </summary>
    /// <param name="consumerProfileId">The identifier of the consumer profile whose orders are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing a collection of <see cref="OrderSummaryResponse"/>
    /// items ordered from most recent to oldest; or a failed result if the profile does not exist.
    /// </returns>
    Task<Result<IEnumerable<OrderSummaryResponse>>> GetConsumerOrdersAsync(int consumerProfileId, CancellationToken ct = default);

    /// <summary>
    /// Returns the detail of a single order belonging to the authenticated consumer.
    /// </summary>
    /// <param name="orderId">The identifier of the order to retrieve.</param>
    /// <param name="consumerProfileId">
    /// The identifier of the consumer profile making the request, used to ensure
    /// consumers can only access their own orders.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing the <see cref="OrderSummaryResponse"/>;
    /// or a failed result if the order does not exist or does not belong to the consumer.
    /// </returns>
    Task<Result<OrderSummaryResponse>> GetOrderByIdAsync(int orderId, int consumerProfileId, CancellationToken ct = default);

    /// <summary>
    /// Returns the order history for the authenticated merchant, including the pickup
    /// status and consumer information for each order.
    /// </summary>
    /// <param name="merchantProfileId">The identifier of the merchant profile whose orders are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing a collection of
    /// <see cref="MerchantOrderSummaryResponse"/> items; or a failed result if the profile does not exist.
    /// </returns>
    Task<Result<IEnumerable<MerchantOrderSummaryResponse>>> GetMerchantOrdersAsync(int merchantProfileId, CancellationToken ct = default);

    /// <summary>
    /// Validates the provided pickup code against pending paid orders for the authenticated
    /// merchant and marks the corresponding order as picked up.
    /// </summary>
    /// <param name="merchantProfileId">The identifier of the merchant profile confirming the pickup.</param>
    /// <param name="pickupCode">The alphanumeric or QR pickup code presented by the consumer at the point of collection.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing the updated <see cref="MerchantOrderSummaryResponse"/>
    /// reflecting the new pickup status; or a failed result if the code is invalid, already used,
    /// or the order does not belong to this merchant.
    /// </returns>
    Task<Result<MerchantOrderSummaryResponse>> ConfirmPickupAsync(int merchantProfileId, string pickupCode, CancellationToken ct = default);
}
