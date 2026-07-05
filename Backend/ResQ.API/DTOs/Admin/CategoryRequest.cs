using System.ComponentModel.DataAnnotations;

namespace ResQ.API.DTOs.Admin;

/// <summary>
/// Request payload for creating or updating a food category.
/// </summary>
public class CategoryRequest
{
    /// <summary>Display name of the category (e.g. "Panadería", "Sushi"). Required, max 100 chars.</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
