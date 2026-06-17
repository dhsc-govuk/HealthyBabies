using Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Extensions;

public static class PaginatedExtension
{
    public static async Task<PaginatedResult<T>> PaginateAsync<T>(this IQueryable<T> source, int page, int pageSize, CancellationToken cancellationToken)
    {
        var count = await source.CountAsync(cancellationToken);
        var items = await source.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PaginatedResult<T>
        {
            Items = items,
            TotalCount = count,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(count / (double)pageSize),
            PageSize = pageSize
        };
    }

    public static Task<PaginatedResult<T>> PaginateAsync<T>(this IEnumerable<T> source, int page, int pageSize, CancellationToken cancellationToken)
    {
        var count = source.Count();
        var items = source.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Task.FromResult(new PaginatedResult<T>
        {
            Items = items,
            TotalCount = count,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(count / (double)pageSize),
            PageSize = pageSize
        });
    }
}