using Keon.Contracts;
using Keon.Contracts.Execution;
using Keon.Contracts.Identifiers;
using Keon.Contracts.Receipts;

namespace Keon.Sdk.Testing.TestFixtures;

/// <summary>
/// Builder for creating test ExecutionRequest instances.
/// </summary>
public sealed class ExecutionRequestBuilder
{
    private int _requestVersion = 1;
    private TenantId _tenantId = new TenantId("test-tenant");
    private CorrelationId _correlationId = CorrelationId.From($"t:test-tenant|c:{Guid.NewGuid()}");
    private DecisionReceiptId _decisionReceiptId = new DecisionReceiptId(Guid.NewGuid().ToString());
    private string? _idempotencyKey;
    private ExecutionTarget _target = new ExecutionTarget("noop");
    private Dictionary<string, object?>? _parameters;
    private Dictionary<string, string>? _tags;

    public ExecutionRequestBuilder WithRequestVersion(int version)
    {
        _requestVersion = version;
        return this;
    }

    public ExecutionRequestBuilder WithTenantId(TenantId tenantId)
    {
        _tenantId = tenantId;
        return this;
    }

    public ExecutionRequestBuilder WithCorrelationId(CorrelationId correlationId)
    {
        _correlationId = correlationId;
        return this;
    }

    public ExecutionRequestBuilder WithDecisionReceiptId(DecisionReceiptId receiptId)
    {
        _decisionReceiptId = receiptId;
        return this;
    }

    public ExecutionRequestBuilder WithIdempotencyKey(string key)
    {
        _idempotencyKey = key;
        return this;
    }

    public ExecutionRequestBuilder WithTarget(ExecutionTarget target)
    {
        _target = target;
        return this;
    }

    public ExecutionRequestBuilder WithParameter(string key, object? value)
    {
        _parameters ??= new Dictionary<string, object?>();
        _parameters[key] = value;
        return this;
    }

    public ExecutionRequestBuilder WithTag(string key, string value)
    {
        _tags ??= new Dictionary<string, string>();
        _tags[key] = value;
        return this;
    }

    public ExecutionRequest Build()
    {
        // Validation: prevent invalid requests
        if (_tenantId.Value == null)
            throw new InvalidOperationException("TenantId cannot be null.");

        if (_correlationId.Value == null)
            throw new InvalidOperationException("CorrelationId cannot be null.");

        if (_decisionReceiptId.Value == null)
            throw new InvalidOperationException("DecisionReceiptId cannot be null. Use WithDecisionReceiptId() to set a valid receipt ID.");

        if (_target == null || string.IsNullOrWhiteSpace(_target.Kind))
            throw new InvalidOperationException("Target cannot be null and must have a valid Kind.");

        return new ExecutionRequest
        {
            RequestVersion = _requestVersion,
            TenantId = _tenantId,
            CorrelationId = _correlationId,
            DecisionReceiptId = _decisionReceiptId,
            IdempotencyKey = _idempotencyKey,
            Target = _target,
            Parameters = _parameters,
            Tags = _tags
        };
    }

    public static ExecutionRequest Default() => new ExecutionRequestBuilder().Build();

    public static ExecutionRequest FromDecisionReceipt(Keon.Contracts.Decision.DecisionReceipt receipt)
    {
        return new ExecutionRequestBuilder()
            .WithTenantId(receipt.Context.TenantId)
            .WithCorrelationId(receipt.Context.CorrelationId)
            .WithDecisionReceiptId(new DecisionReceiptId(receipt.ReceiptId.Value))
            .Build();
    }
}
