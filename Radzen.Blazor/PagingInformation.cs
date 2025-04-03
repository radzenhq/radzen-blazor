namespace Radzen.Blazor
{
    /// <summary>
    /// Represents paging information.
    /// </summary>
    /// <param name="CurrentPage">The current page number.</param>
    /// <param name="NumberOfPages">The total number of pages.</param>
    /// <param name="TotalCount">The total count of items.</param>
    public record PagingInformation(int CurrentPage, int NumberOfPages, int TotalCount);
}
