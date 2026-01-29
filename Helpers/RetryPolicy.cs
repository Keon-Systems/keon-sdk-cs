using Keon.Contracts.Results;

namespace Keon.Sdk.Helpers;

/// <summary>
/// Immutable retry policy with exponential backoff and jitter.
/// Handles transient failures based on KeonResult error codes.
/// Use static factory methods to create safe, pre-configured policies.
/// </summary>
public sealed class RetryPolicy
{
    private readonly int _maxAttempts;
    private readonly TimeSpan _initialDelay;
    private readonly TimeSpan _maxDelay;
    private readonly double _backoffMultiplier;

    // Private constructor - force use of factory methods
    private RetryPolicy(
        int maxAttempts,
        TimeSpan initialDelay,
        TimeSpan maxDelay,
        double backoffMultiplier)
    {
        _maxAttempts = maxAttempts;
        _initialDelay = initialDelay;
        _maxDelay = maxDelay;
        _backoffMultiplier = backoffMultiplier;
    }

    /// <summary>
    /// Default transient error detection: timeout, throttle, temporary unavailability.
    /// </summary>
    private static bool DefaultIsTransient(string? errorCode)
    {
        if (string.IsNullOrEmpty(errorCode))
            return false;

        return errorCode switch
        {
            "TIMEOUT" => true,
            "THROTTLED" => true,
            "SERVICE_UNAVAILABLE" => true,
            "TEMPORARY_FAILURE" => true,
            _ => errorCode.StartsWith("TRANSIENT_", StringComparison.OrdinalIgnoreCase)
        };
    }

    /// <summary>
    /// Execute an operation with retry logic.
    /// </summary>
    public async Task<KeonResult<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<KeonResult<T>>> operation,
        CancellationToken ct = default)
    {
        var attempt = 0;
        KeonResult<T>? lastResult = null;

        while (attempt < _maxAttempts)
        {
            attempt++;

            try
            {
                lastResult = await operation(ct).ConfigureAwait(false);

                if (lastResult.Success)
                    return lastResult;

                // Check if error is transient
                if (!DefaultIsTransient(lastResult.ErrorCode))
                    return lastResult; // Non-transient failure, return immediately

                // Last attempt failed, don't delay
                if (attempt >= _maxAttempts)
                    break;

                // Calculate delay with exponential backoff
                var delay = CalculateDelay(attempt - 1);
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw; // Don't retry cancellations
            }
            catch (Exception ex)
            {
                // Unexpected exception - wrap and return
                return KeonResult<T>.Fail(
                    "RETRY_EXCEPTION",
                    $"Exception during retry attempt {attempt}: {ex.Message}");
            }
        }

        // All retries exhausted
        return lastResult ?? KeonResult<T>.Fail(
            "RETRY_EXHAUSTED",
            $"Operation failed after {_maxAttempts} attempts");
    }

    private TimeSpan CalculateDelay(int attemptIndex)
    {
        var baseDelay = _initialDelay.TotalMilliseconds * Math.Pow(_backoffMultiplier, attemptIndex);
        baseDelay = Math.Min(baseDelay, _maxDelay.TotalMilliseconds);

        // Always use jitter to prevent thundering herd
        var jitter = Random.Shared.NextDouble() * 0.5 - 0.25; // -0.25 to +0.25
        baseDelay *= (1.0 + jitter);

        return TimeSpan.FromMilliseconds(baseDelay);
    }

    /// <summary>
    /// Create a default retry policy for gateway operations.
    /// 3 attempts, 100ms initial delay, 10s max delay, 2x backoff.
    /// </summary>
    public static RetryPolicy Default() => new(
        maxAttempts: 3,
        initialDelay: TimeSpan.FromMilliseconds(100),
        maxDelay: TimeSpan.FromSeconds(10),
        backoffMultiplier: 2.0);

    /// <summary>
    /// Create a conservative retry policy for non-critical operations.
    /// 2 attempts, 200ms initial delay, 5s max delay, 2x backoff.
    /// </summary>
    public static RetryPolicy Conservative() => new(
        maxAttempts: 2,
        initialDelay: TimeSpan.FromMilliseconds(200),
        maxDelay: TimeSpan.FromSeconds(5),
        backoffMultiplier: 2.0);

    /// <summary>
    /// No retries - fail fast.
    /// Use for operations that should not be retried (e.g., validation errors).
    /// </summary>
    public static RetryPolicy NoRetry() => new(
        maxAttempts: 1,
        initialDelay: TimeSpan.Zero,
        maxDelay: TimeSpan.Zero,
        backoffMultiplier: 1.0);
}
