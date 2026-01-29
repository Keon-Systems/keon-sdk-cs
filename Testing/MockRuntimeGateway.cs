using Keon.Contracts;
using Keon.Contracts.Decision;
using Keon.Contracts.Execution;
using Keon.Contracts.Results;
using Keon.Runtime.Sdk;

namespace Keon.Sdk.Testing;

/// <summary>
/// Manual mock for IRuntimeGateway without external dependencies.
/// Configure responses via public properties and methods.
/// </summary>
public sealed class MockRuntimeGateway : IRuntimeGateway
{
    private readonly Queue<KeonResult<DecisionReceipt>> _decisionResponses = new();
    private readonly Queue<KeonResult<ExecutionResult>> _executionResponses = new();

    public List<DecisionRequest> DecisionRequests { get; } = new();
    public List<ExecutionRequest> ExecutionRequests { get; } = new();

    public int DecisionCallCount => DecisionRequests.Count;
    public int ExecutionCallCount => ExecutionRequests.Count;

    /// <summary>
    /// Enqueue a decision response. Responses are returned in FIFO order.
    /// </summary>
    public void EnqueueDecisionResponse(KeonResult<DecisionReceipt> response)
    {
        _decisionResponses.Enqueue(response);
    }

    /// <summary>
    /// Enqueue an execution response. Responses are returned in FIFO order.
    /// </summary>
    public void EnqueueExecutionResponse(KeonResult<ExecutionResult> response)
    {
        _executionResponses.Enqueue(response);
    }

    /// <summary>
    /// Configure the mock to always return an approved decision.
    /// </summary>
    public void AlwaysApprove()
    {
        _decisionResponses.Clear();
        // Will use default success behavior
    }

    /// <summary>
    /// Configure the mock to always deny decisions with a specific reason.
    /// </summary>
    public void AlwaysDeny(string reason = "MOCK_DENY")
    {
        _decisionResponses.Clear();
        EnqueueDecisionResponse(KeonResult<DecisionReceipt>.Fail("POLICY_DENY", reason));
    }

    /// <summary>
    /// Reset all captured requests and queued responses.
    /// </summary>
    public void Reset()
    {
        DecisionRequests.Clear();
        ExecutionRequests.Clear();
        _decisionResponses.Clear();
        _executionResponses.Clear();
    }

    public Task<KeonResult<DecisionReceipt>> DecideAsync(
        DecisionRequest request,
        CancellationToken ct = default)
    {
        DecisionRequests.Add(request);

        if (_decisionResponses.Count > 0)
        {
            return Task.FromResult(_decisionResponses.Dequeue());
        }

        // Default: return approved decision
        var receipt = TestFixtures.DecisionReceipts.CreateApproved(
            request.RequestId,
            request.Capability);

        return Task.FromResult(KeonResult<DecisionReceipt>.Ok(receipt));
    }

    public Task<KeonResult<ExecutionResult>> ExecuteAsync(
        ExecutionRequest request,
        CancellationToken ct = default)
    {
        ExecutionRequests.Add(request);

        if (_executionResponses.Count > 0)
        {
            return Task.FromResult(_executionResponses.Dequeue());
        }

        // Default: return completed execution
        var result = TestFixtures.ExecutionResults.CreateCompleted(request.CorrelationId);

        return Task.FromResult(KeonResult<ExecutionResult>.Ok(result));
    }

    /// <summary>
    /// Verify that DecideAsync was called with a specific capability.
    /// </summary>
    public bool WasCalledWithCapability(string capability) =>
        DecisionRequests.Any(r => r.Capability == capability);

    /// <summary>
    /// Get the last decision request, or null if none.
    /// </summary>
    public DecisionRequest? LastDecisionRequest =>
        DecisionRequests.Count > 0 ? DecisionRequests[^1] : null;

    /// <summary>
    /// Get the last execution request, or null if none.
    /// </summary>
    public ExecutionRequest? LastExecutionRequest =>
        ExecutionRequests.Count > 0 ? ExecutionRequests[^1] : null;
}
