using Keon.Contracts;
using Keon.Contracts.Decision;
using Keon.Contracts.Execution;
using Keon.Contracts.Results;
using Keon.Runtime.Sdk;
using Keon.Sdk.Discovery;

namespace Keon.Sdk.Testing;

/// <summary>
/// Lightweight in-memory fake gateway with deterministic behavior.
/// Simulates basic runtime logic without external dependencies.
/// Implements IRuntimeGatewayIntrospection for capability discovery testing.
/// </summary>
public sealed class InMemoryRuntimeGateway : IRuntimeGateway, IRuntimeGatewayIntrospection
{
    private readonly Dictionary<string, PolicyEffect> _capabilityPolicies = new();
    private readonly List<DecisionReceipt> _decisionHistory = new();
    private readonly List<ExecutionResult> _executionHistory = new();

    public IReadOnlyList<DecisionReceipt> DecisionHistory => _decisionHistory.AsReadOnly();
    public IReadOnlyList<ExecutionResult> ExecutionHistory => _executionHistory.AsReadOnly();

    public TimeSpan SimulatedLatency { get; set; } = TimeSpan.Zero;
    public bool SimulateFailures { get; set; } = false;
    public double FailureRate { get; set; } = 0.1; // 10% by default

    /// <summary>
    /// Configure policy effect for a specific capability.
    /// </summary>
    public void SetCapabilityPolicy(string capability, PolicyEffect effect)
    {
        _capabilityPolicies[capability] = effect;
    }

    /// <summary>
    /// Reset all state and history.
    /// </summary>
    public void Reset()
    {
        _capabilityPolicies.Clear();
        _decisionHistory.Clear();
        _executionHistory.Clear();
    }

    public async Task<KeonResult<DecisionReceipt>> DecideAsync(
        DecisionRequest request,
        CancellationToken ct = default)
    {
        if (SimulatedLatency > TimeSpan.Zero)
            await Task.Delay(SimulatedLatency, ct).ConfigureAwait(false);

        if (SimulateFailures && Random.Shared.NextDouble() < FailureRate)
            return KeonResult<DecisionReceipt>.Fail("SIMULATED_FAILURE", "Test failure simulation");

        // Determine policy effect
        var effect = _capabilityPolicies.GetValueOrDefault(request.Capability, PolicyEffect.Allow);

        DecisionReceipt receipt = effect switch
        {
            PolicyEffect.Allow => TestFixtures.DecisionReceipts.CreateApproved(
                request.RequestId,
                request.Capability),

            PolicyEffect.Deny => TestFixtures.DecisionReceipts.CreateDenied(
                request.RequestId,
                request.Capability,
                "POLICY_DENY"),

            PolicyEffect.RequireHumanApproval => TestFixtures.DecisionReceipts.CreateNeedsHumanReview(
                request.RequestId,
                request.Capability),

            _ => TestFixtures.DecisionReceipts.CreateApproved(request.RequestId, request.Capability)
        };

        _decisionHistory.Add(receipt);

        return KeonResult<DecisionReceipt>.Ok(receipt);
    }

    public async Task<KeonResult<ExecutionResult>> ExecuteAsync(
        ExecutionRequest request,
        CancellationToken ct = default)
    {
        if (SimulatedLatency > TimeSpan.Zero)
            await Task.Delay(SimulatedLatency, ct).ConfigureAwait(false);

        if (SimulateFailures && Random.Shared.NextDouble() < FailureRate)
            return KeonResult<ExecutionResult>.Fail("SIMULATED_FAILURE", "Test failure simulation");

        // Verify decision exists in history
        var decisionExists = _decisionHistory.Any(d =>
            d.ReceiptId.Value == request.DecisionReceiptId.Value);

        if (!decisionExists)
        {
            return KeonResult<ExecutionResult>.Fail(
                "INVALID_DECISION_RECEIPT",
                $"Decision receipt {request.DecisionReceiptId.Value} not found in history");
        }

        var result = TestFixtures.ExecutionResults.CreateCompleted(request.CorrelationId);
        _executionHistory.Add(result);

        return KeonResult<ExecutionResult>.Ok(result);
    }

    /// <summary>
    /// Get all decisions for a specific capability.
    /// </summary>
    public IEnumerable<DecisionReceipt> GetDecisionsForCapability(string capability) =>
        _decisionHistory.Where(d => d.Capability == capability);

    // IRuntimeGatewayIntrospection implementation

    public Task<IReadOnlyList<CapabilityId>> GetCapabilitiesAsync(CancellationToken ct = default)
    {
        var capabilities = _capabilityPolicies.Keys
            .Select(c => CapabilityId.From(c))
            .ToList();

        return Task.FromResult<IReadOnlyList<CapabilityId>>(capabilities);
    }

    public Task<bool> SupportsCapabilityAsync(CapabilityId capability, CancellationToken ct = default)
    {
        var supported = _capabilityPolicies.ContainsKey(capability.Value);
        return Task.FromResult(supported);
    }
}
