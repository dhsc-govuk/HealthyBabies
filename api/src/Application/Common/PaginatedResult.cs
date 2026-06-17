namespace Application.Common;

public class PaginatedResult<T>
{
    public required IList<T> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int CurrentPage { get; init; }
    public required int TotalPages { get; init; }
    public required int PageSize { get; init; }

    public int UrgentImagesCount { get; init; }

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}