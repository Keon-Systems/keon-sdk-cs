namespace Keon.Sdk.Helpers;

/// <summary>
/// Generic pagination helpers for cursor-based and offset-based patterns.
/// </summary>
public static class Pagination
{
    /// <summary>
    /// Iterate through all pages using a cursor-based fetch function.
    /// </summary>
    /// <typeparam name="T">Item type</typeparam>
    /// <param name="fetchPage">Function that fetches a page given a cursor (null for first page)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Async enumerable of all items across all pages</returns>
    public static async IAsyncEnumerable<T> EnumerateAllAsync<T>(
        Func<string?, CancellationToken, Task<PageResult<T>>> fetchPage,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        string? cursor = null;

        do
        {
            var page = await fetchPage(cursor, ct).ConfigureAwait(false);

            foreach (var item in page.Items)
            {
                yield return item;
            }

            cursor = page.NextCursor;

        } while (cursor is not null);
    }

    /// <summary>
    /// Fetch all pages into a single list (use with caution for large result sets).
    /// </summary>
    public static async Task<List<T>> FetchAllAsync<T>(
        Func<string?, CancellationToken, Task<PageResult<T>>> fetchPage,
        CancellationToken ct = default)
    {
        var results = new List<T>();
        string? cursor = null;

        do
        {
            var page = await fetchPage(cursor, ct).ConfigureAwait(false);
            results.AddRange(page.Items);
            cursor = page.NextCursor;

        } while (cursor is not null);

        return results;
    }

    /// <summary>
    /// Create a paginated batch processor with max concurrency.
    /// Processes each page as it arrives.
    /// </summary>
    public static async Task ProcessPagesAsync<T>(
        Func<string?, CancellationToken, Task<PageResult<T>>> fetchPage,
        Func<IReadOnlyList<T>, CancellationToken, Task> processPage,
        CancellationToken ct = default)
    {
        string? cursor = null;

        do
        {
            var page = await fetchPage(cursor, ct).ConfigureAwait(false);

            if (page.Items.Count > 0)
            {
                await processPage(page.Items, ct).ConfigureAwait(false);
            }

            cursor = page.NextCursor;

        } while (cursor is not null);
    }
}

/// <summary>
/// Standard page result structure.
/// </summary>
public sealed record PageResult<T>(
    IReadOnlyList<T> Items,
    string? NextCursor = null,
    int? TotalCount = null)
{
    public bool HasMore => NextCursor is not null;
}

/// <summary>
/// Offset-based pagination helpers (less preferred, use cursor-based when possible).
/// </summary>
public static class OffsetPagination
{
    /// <summary>
    /// Iterate through all pages using offset/limit pattern.
    /// </summary>
    public static async IAsyncEnumerable<T> EnumerateAllAsync<T>(
        Func<int, int, CancellationToken, Task<OffsetPageResult<T>>> fetchPage,
        int pageSize = 100,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        int offset = 0;
        bool hasMore = true;

        while (hasMore)
        {
            var page = await fetchPage(offset, pageSize, ct).ConfigureAwait(false);

            foreach (var item in page.Items)
            {
                yield return item;
            }

            offset += page.Items.Count;
            hasMore = page.HasMore;
        }
    }
}

/// <summary>
/// Offset-based page result.
/// </summary>
public sealed record OffsetPageResult<T>(
    IReadOnlyList<T> Items,
    int Offset,
    int Limit,
    int? TotalCount = null)
{
    public bool HasMore => TotalCount.HasValue
        ? Offset + Items.Count < TotalCount.Value
        : Items.Count >= Limit;
}
