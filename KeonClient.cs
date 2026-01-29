using Keon.Contracts;
using Keon.Contracts.Decision;
using Keon.Contracts.Execution;
using Keon.Contracts.Results;
using Keon.Runtime.Sdk;
using Keon.Sdk.Helpers;

namespace Keon.Sdk;

/// <summary>
/// Safe-by-default client for interacting with Keon Runtime.
/// Automatically handles retries, receipt tracking, and validation.
/// This is the recommended entry point for all SDK usage.
/// </summary>
public sealed class KeonClient : IDisposable
{
    private readonly IRuntimeGateway _gateway;
    private readonly RetryPolicy _retryPolicy;
    private readonly List<DecisionReceipt> _receiptHistory;
    private readonly SemaphoreSlim _lock;
    private bool _disposed;

    /// <summary>
    /// Create a new KeonClient with safe defaults.
    /// </summary>
    /// <param name="gateway">The runtime gateway to use</param>
    /// <param name="retryPolicy">Optional retry policy (uses safe default if not provided)</param>
    public KeonClient(IRuntimeGateway gateway, RetryPolicy? retryPolicy = null)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _retryPolicy = retryPolicy ?? RetryPolicy.Default();
        _receiptHistory = new List<DecisionReceipt>();
        _lock = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// All decision receipts processed by this client (read-only).
    /// Receipts are automatically tracked for audit purposes.
    /// </summary>
    public IReadOnlyList<DecisionReceipt> ReceiptHistory
    {
        get
        {
            ThrowIfDisposed();
            lock (_receiptHistory)
            {
                return _receiptHistory.AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Request a decision from the runtime.
    /// Automatically retries on transient failures and tracks the receipt.
    /// </summary>
    public async Task<KeonResult<DecisionReceipt>> DecideAsync(
        DecisionRequest request,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ValidateDecisionRequest(request);

        var result = await _retryPolicy.ExecuteAsync(
            ct => _gateway.DecideAsync(request, ct),
            ct).ConfigureAwait(false);

        // Track successful receipts
        if (result.Success && result.Value is not null)
        {
            await TrackReceiptAsync(result.Value, ct).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Execute an approved decision.
    /// Automatically retries on transient failures.
    /// </summary>
    public async Task<KeonResult<ExecutionResult>> ExecuteAsync(
        ExecutionRequest request,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ValidateExecutionRequest(request);

        return await _retryPolicy.ExecuteAsync(
            ct => _gateway.ExecuteAsync(request, ct),
            ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Decide and execute in a single operation (convenience method).
    /// Only executes if the decision is approved.
    /// </summary>
    public async Task<KeonResult<ExecutionResult>> DecideAndExecuteAsync(
        DecisionRequest decisionRequest,
        Func<DecisionReceipt, ExecutionRequest> buildExecutionRequest,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var decisionResult = await DecideAsync(decisionRequest, ct).ConfigureAwait(false);
        if (!decisionResult.Success || decisionResult.Value is null)
        {
            return KeonResult<ExecutionResult>.Fail(
                decisionResult.ErrorCode ?? "DECISION_FAILED",
                decisionResult.ErrorMessage ?? "Decision failed");
        }

        var receipt = decisionResult.Value;

        // Only execute if approved
        if (receipt.Outcome != DecisionOutcome.Approved)
        {
            return KeonResult<ExecutionResult>.Fail(
                "NOT_APPROVED",
                $"Decision outcome was {receipt.Outcome}, not Approved");
        }

        var executionRequest = buildExecutionRequest(receipt);
        return await ExecuteAsync(executionRequest, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Get all receipts for a specific capability.
    /// </summary>
    public IReadOnlyList<DecisionReceipt> GetReceiptsForCapability(string capability)
    {
        ThrowIfDisposed();
        lock (_receiptHistory)
        {
            return _receiptHistory
                .Where(r => r.Capability == capability)
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// Clear receipt history (use with caution - for testing only).
    /// </summary>
    public void ClearReceiptHistory()
    {
        ThrowIfDisposed();
        lock (_receiptHistory)
        {
            _receiptHistory.Clear();
        }
    }

    private async Task TrackReceiptAsync(DecisionReceipt receipt, CancellationToken ct)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            _receiptHistory.Add(receipt);
        }
        finally
        {
            _lock.Release();
        }
    }

    private static void ValidateDecisionRequest(DecisionRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.Capability))
            throw new ArgumentException("Capability cannot be null or empty", nameof(request));
        if (request.RequestId.Value == null)
            throw new ArgumentException("RequestId cannot be null", nameof(request));
    }

    private static void ValidateExecutionRequest(ExecutionRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));
        if (request.DecisionReceiptId.Value == null)
            throw new ArgumentException("DecisionReceiptId cannot be null", nameof(request));
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(KeonClient));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _lock.Dispose();
        _disposed = true;
    }
}

