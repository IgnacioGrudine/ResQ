namespace ResQ.API.DTOs.Shared;

/// <summary>
/// Shared response object representing a food category, used across merchant,
/// product, and catalog endpoints.
/// </summary>
public class CategoryResponse
{
    /// <summary>
    /// Unique identifier of the category.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Display name of the category (e.g., "Bakery", "Sushi", "Restaurant").
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
