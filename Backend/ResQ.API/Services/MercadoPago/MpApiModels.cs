using System.Text.Json.Serialization;

namespace ResQ.API.Services.MercadoPago;

// Internal records used for MP REST API serialization/deserialization.
// Not exposed to controllers or clients.

internal sealed record MpPreferenceRequest(
    [property: JsonPropertyName("items")]              List<MpItem>    Items,
    [property: JsonPropertyName("marketplace_fee")]    decimal         MarketplaceFee,
    [property: JsonPropertyName("external_reference")] string          ExternalReference,
    [property: JsonPropertyName("back_urls")]          MpBackUrls      BackUrls,
    [property: JsonPropertyName("auto_return")]        string          AutoReturn,
    [property: JsonPropertyName("notification_url")]   string          NotificationUrl
);

internal sealed record MpItem(
    [property: JsonPropertyName("title")]       string  Title,
    [property: JsonPropertyName("quantity")]    int     Quantity,
    [property: JsonPropertyName("currency_id")] string  CurrencyId,
    [property: JsonPropertyName("unit_price")]  decimal UnitPrice
);

internal sealed record MpBackUrls(
    [property: JsonPropertyName("success")] string Success,
    [property: JsonPropertyName("failure")] string Failure,
    [property: JsonPropertyName("pending")] string Pending
);

internal sealed record MpPreferenceApiResponse(
    [property: JsonPropertyName("id")]                   string Id,
    [property: JsonPropertyName("init_point")]           string InitPoint,
    [property: JsonPropertyName("sandbox_init_point")]   string SandboxInitPoint
);

internal sealed record MpPaymentApiResponse(
    [property: JsonPropertyName("id")]                 long   Id,
    [property: JsonPropertyName("status")]             string Status,
    [property: JsonPropertyName("external_reference")] string ExternalReference,
    [property: JsonPropertyName("transaction_amount")] decimal TransactionAmount
);
