namespace Keon.Sdk.Discovery;

/// <summary>
/// Optional capability discovery interface for runtime gateways.
/// SDK-only: Runtime implementations can choose to implement this for capability introspection.
/// </summary>
public interface IRuntimeGatewayIntrospection
{
    /// <summary>
    /// Get all capabilities supported by this runtime instance.
    /// Returns empty list if capability discovery is not supported.
    /// </summary>
    Task<IReadOnlyList<CapabilityId>> GetCapabilitiesAsync(CancellationToken ct = default);

    /// <summary>
    /// Check if a specific capability is supported.
    /// Returns false if capability discovery is not supported.
    /// </summary>
    Task<bool> SupportsCapabilityAsync(CapabilityId capability, CancellationToken ct = default);
}
