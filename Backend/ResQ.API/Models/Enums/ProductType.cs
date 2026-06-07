namespace ResQ.API.Models.Enums;

/// <summary>
/// Classifies a product listing by how its contents are presented to the consumer.
/// </summary>
public enum ProductType : byte
{
    /// <summary>
    /// A Surprise Pack (Pack Sorpresa) — the merchant's primary offering on ResQ.
    /// The exact contents are not disclosed before purchase; the consumer receives
    /// a curated selection of surplus food items determined by the merchant at pickup time.
    /// </summary>
    SurprisePack = 1,

    /// <summary>
    /// An explicitly described product whose contents are fully disclosed to the consumer
    /// before purchase. Used for listings where transparency about the item is preferred.
    /// </summary>
    ExplicitItem = 2
}
