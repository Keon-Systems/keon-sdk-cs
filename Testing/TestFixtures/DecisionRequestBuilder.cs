using Keon.Contracts;
using Keon.Contracts.Decision;
using Keon.Contracts.Identifiers;

namespace Keon.Sdk.Testing.TestFixtures;

/// <summary>
/// Builder for creating test DecisionRequest instances.
/// </summary>
public sealed class DecisionRequestBuilder
{
    private RequestId _requestId = new RequestId(Guid.NewGuid().ToString());
    private TenantId _tenantId = new TenantId("test-tenant");
    private ActorId _actorId = new ActorId("test-actor");
    private string _capability = "test.capability";
    private Dictionary<string, object?> _input = new();
    private DecisionContext? _context;
    private DateTimeOffset _requestedAtUtc = DateTimeOffset.UtcNow;

    public DecisionRequestBuilder WithRequestId(RequestId requestId)
    {
        _requestId = requestId;
        return this;
    }

    public DecisionRequestBuilder WithTenantId(TenantId tenantId)
    {
        _tenantId = tenantId;
        return this;
    }

    public DecisionRequestBuilder WithActorId(ActorId actorId)
    {
        _actorId = actorId;
        return this;
    }

    public DecisionRequestBuilder WithCapability(string capability)
    {
        _capability = capability;
        return this;
    }

    public DecisionRequestBuilder WithInput(string key, object? value)
    {
        _input[key] = value;
        return this;
    }

    public DecisionRequestBuilder WithInput(IReadOnlyDictionary<string, object?> input)
    {
        _input = new Dictionary<string, object?>(input);
        return this;
    }

    public DecisionRequestBuilder WithContext(DecisionContext context)
    {
        _context = context;
        return this;
    }

    public DecisionRequestBuilder WithRequestedAtUtc(DateTimeOffset timestamp)
    {
        _requestedAtUtc = timestamp;
        return this;
    }

    public DecisionRequest Build()
    {
        // Validation: prevent invalid requests
        if (string.IsNullOrWhiteSpace(_capability))
            throw new InvalidOperationException("Capability cannot be null or empty. Use WithCapability() to set a valid capability.");

        if (_requestId.Value == null)
            throw new InvalidOperationException("RequestId cannot be null.");

        if (_tenantId.Value == null)
            throw new InvalidOperationException("TenantId cannot be null.");

        if (_actorId.Value == null)
            throw new InvalidOperationException("ActorId cannot be null.");

        var context = _context ?? new DecisionContext(
            TenantId: _tenantId,
            CorrelationId: CorrelationId.From($"t:{_tenantId.Value}|c:{Guid.NewGuid()}"),
            SessionId: null,
            ClientApp: "test-client",
            Environment: "test",
            Tags: Array.Empty<string>(),
            Operation: null);

        return new DecisionRequest(
            RequestId: _requestId,
            TenantId: _tenantId,
            ActorId: _actorId,
            Capability: _capability,
            Input: _input,
            Context: context,
            RequestedAtUtc: _requestedAtUtc);
    }

    public static DecisionRequest Default() => new DecisionRequestBuilder().Build();

    public static DecisionRequest ForCapability(string capability) =>
        new DecisionRequestBuilder().WithCapability(capability).Build();
}
