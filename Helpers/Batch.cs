using Keon.Contracts.Results;

namespace Keon.Sdk.Helpers;

/// <summary>
/// Batching helpers for processing multiple requests with controlled concurrency.
/// Enforces hard limits to prevent resource exhaustion.
/// </summary>
public static class Batch
{
    /// <summary>
    /// Maximum allowed concurrency to prevent resource exhaustion.
    /// </summary>
    public const int MaxConcurrency = 50;

    /// <summary>
    /// Maximum allowed batch size to prevent memory issues.
    /// </summary>
    public const int MaxBatchSize = 1000;

    /// <summary>
    /// Default concurrency for batch operations.
    /// </summary>
    public const int DefaultConcurrency = 10;

    /// <summary>
    /// Execute multiple operations in parallel with max concurrency limit.
    /// Returns all results, including failures.
    /// Concurrency is capped at 50 to prevent resource exhaustion.
    /// </summary>
    public static async Task<IReadOnlyList<KeonResult<TResult>>> ExecuteAsync<TInput, TResult>(
        IEnumerable<TInput> items,
        Func<TInput, CancellationToken, Task<KeonResult<TResult>>> operation,
        int maxConcurrency = DefaultConcurrency,
        CancellationToken ct = default)
    {
        if (maxConcurrency < 1)
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Must be at least 1");
        if (maxConcurrency > MaxConcurrency)
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency), $"Cannot exceed {MaxConcurrency}");

        var itemsList = items.ToList();

        // Enforce batch size limit
        if (itemsList.Count > MaxBatchSize)
            throw new ArgumentException($"Batch size {itemsList.Count} exceeds maximum of {MaxBatchSize}", nameof(items));

        var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var tasks = new List<Task<KeonResult<TResult>>>(itemsList.Count);

        foreach (var item in itemsList)
        {
            await semaphore.WaitAsync(ct).ConfigureAwait(false);

            var task = Task.Run(async () =>
            {
                try
                {
                    return await operation(item, ct).ConfigureAwait(false);
                }
                finally
                {
                    semaphore.Release();
                }
            }, ct);

            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        semaphore.Dispose();

        return results;
    }

    /// <summary>
    /// Execute operations in batches of specified size.
    /// Processes each batch sequentially, but items within a batch run in parallel.
    /// Batch size is capped at 100 to prevent memory issues.
    /// </summary>
    public static async Task<IReadOnlyList<KeonResult<TResult>>> ExecuteInBatchesAsync<TInput, TResult>(
        IEnumerable<TInput> items,
        Func<TInput, CancellationToken, Task<KeonResult<TResult>>> operation,
        int batchSize = DefaultConcurrency,
        CancellationToken ct = default)
    {
        if (batchSize < 1)
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Must be at least 1");
        if (batchSize > 100)
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Cannot exceed 100");

        var results = new List<KeonResult<TResult>>();
        var batch = new List<TInput>(batchSize);

        foreach (var item in items)
        {
            batch.Add(item);

            if (batch.Count >= batchSize)
            {
                var batchResults = await ExecuteAsync(batch, operation, batchSize, ct).ConfigureAwait(false);
                results.AddRange(batchResults);
                batch.Clear();
            }
        }

        // Process remaining items
        if (batch.Count > 0)
        {
            var batchResults = await ExecuteAsync(batch, operation, batchSize, ct).ConfigureAwait(false);
            results.AddRange(batchResults);
        }

        return results;
    }

    /// <summary>
    /// Execute operations and collect only successful results.
    /// Failed operations are logged to the provided callback (optional).
    /// Concurrency is capped at 50 to prevent resource exhaustion.
    /// </summary>
    public static async Task<IReadOnlyList<TResult>> ExecuteAndCollectSuccessAsync<TInput, TResult>(
        IEnumerable<TInput> items,
        Func<TInput, CancellationToken, Task<KeonResult<TResult>>> operation,
        int maxConcurrency = DefaultConcurrency,
        Action<TInput, KeonResult<TResult>>? onFailure = null,
        CancellationToken ct = default)
    {
        var results = await ExecuteAsync(items, operation, maxConcurrency, ct).ConfigureAwait(false);
        var successes = new List<TResult>(results.Count);
        var itemsList = items.ToList();

        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            if (result.Success && result.Value is not null)
            {
                successes.Add(result.Value);
            }
            else if (onFailure is not null)
            {
                onFailure(itemsList[i], result);
            }
        }

        return successes;
    }

    /// <summary>
    /// Execute operations with progress reporting.
    /// Concurrency is capped at 50 to prevent resource exhaustion.
    /// </summary>
    public static async Task<IReadOnlyList<KeonResult<TResult>>> ExecuteWithProgressAsync<TInput, TResult>(
        IEnumerable<TInput> items,
        Func<TInput, CancellationToken, Task<KeonResult<TResult>>> operation,
        int maxConcurrency = DefaultConcurrency,
        IProgress<BatchProgress>? progress = null,
        CancellationToken ct = default)
    {
        var itemsList = items.ToList();
        var total = itemsList.Count;
        var completed = 0;
        var succeeded = 0;
        var failed = 0;

        progress?.Report(new BatchProgress(0, total, 0, 0));

        var results = await ExecuteAsync(
            itemsList,
            async (item, ct) =>
            {
                var result = await operation(item, ct).ConfigureAwait(false);

                Interlocked.Increment(ref completed);
                if (result.Success)
                    Interlocked.Increment(ref succeeded);
                else
                    Interlocked.Increment(ref failed);

                progress?.Report(new BatchProgress(completed, total, succeeded, failed));

                return result;
            },
            maxConcurrency,
            ct).ConfigureAwait(false);

        return results;
    }
}

/// <summary>
/// Progress information for batch operations.
/// </summary>
public sealed record BatchProgress(
    int Completed,
    int Total,
    int Succeeded,
    int Failed)
{
    public double PercentComplete => Total > 0 ? (double)Completed / Total * 100 : 0;
    public bool IsComplete => Completed >= Total;
}
