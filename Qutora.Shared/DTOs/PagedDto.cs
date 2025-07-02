namespace Qutora.Shared.DTOs;

/// <summary>
/// Data transfer object for pagination results
/// </summary>
/// <typeparam name="T">Type of list content</typeparam>
public class PagedDto<T>
{
    /// <summary>
    /// List of data items
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Total number of records
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Current page (for UI compatibility)
    /// </summary>
    public int CurrentPage => PageNumber;

    /// <summary>
    /// Number of records per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}