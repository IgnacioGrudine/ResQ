namespace ResQ.API.DTOs.Shared;

/// <summary>
/// Generic envelope for a paginated list endpoint. Carries the current page of items
/// together with the metadata the client needs to render pagination controls.
/// </summary>
/// <typeparam name="T">The type of the items in the page.</typeparam>
public class PagedResponse<T>
{
    /// <summary>The items belonging to the current page.</summary>
    public List<T> Items { get; set; } = [];

    /// <summary>The 1-based index of the current page.</summary>
    public int Page { get; set; }

    /// <summary>The maximum number of items per page.</summary>
    public int PageSize { get; set; }

    /// <summary>The total number of items across all pages (before pagination).</summary>
    public int TotalItems { get; set; }

    /// <summary>The total number of pages given <see cref="TotalItems"/> and <see cref="PageSize"/>.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 0;
}
