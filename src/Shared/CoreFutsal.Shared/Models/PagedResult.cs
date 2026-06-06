namespace CoreFutsal.Shared.Models;

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    public static PagedResult<T> FromList(IReadOnlyList<T> source, int page, int pageSize)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return new PagedResult<T>
        {
            Items      = source.Skip((page - 1) * pageSize).Take(pageSize),
            Page       = page,
            PageSize   = pageSize,
            TotalCount = source.Count
        };
    }

    public static async Task<PagedResult<T>> CreateAsync(
        IQueryable<T> query, int page, int pageSize, CancellationToken ct = default)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .CountAsync(query, ct);
        var items = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(query.Skip((page - 1) * pageSize).Take(pageSize), ct);

        return new PagedResult<T>
        {
            Items      = items,
            Page       = page,
            PageSize   = pageSize,
            TotalCount = total
        };
    }
}
