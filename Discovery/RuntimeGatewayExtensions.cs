using System.Diagnostics;
using Keon.Runtime.Sdk;

namespace Keon.Sdk.Discovery;

/// <summary>
/// Extension methods for capability discovery on IRuntimeGateway.
/// Provides fallback behavior when gateway doesn't implement IRuntimeGatewayIntrospection.
/// </summary>
public static class RuntimeGatewayExtensions
{
    /// <summary>
    /// Get all capabilities supported by the runtime gateway.
    /// Returns empty list if the gateway doesn't implement IRuntimeGatewayIntrospection.
    /// </summary>
    public static async Task<IReadOnlyList<CapabilityId>> GetCapabilitiesAsync(
        this IRuntimeGateway gateway,
        CancellationToken ct = default)
    {
        if (gateway is IRuntimeGatewayIntrospection introspection)
        {
            return await introspection.GetCapabilitiesAsync(ct).ConfigureAwait(false);
        }

        // Fallback: gateway doesn't support introspection
        Debug.WriteLine(
            "Warning: Gateway does not implement IRuntimeGatewayIntrospection. " +
            "Capability discovery is not available.");

        return Array.Empty<CapabilityId>();
    }

    /// <summary>
    /// Check if the runtime gateway supports a specific capability.
    /// Returns false if the gateway doesn't implement IRuntimeGatewayIntrospection.
    /// </summary>
    public static async Task<bool> SupportsCapabilityAsync(
        this IRuntimeGateway gateway,
        CapabilityId capability,
        CancellationToken ct = default)
    {
        if (gateway is IRuntimeGatewayIntrospection introspection)
        {
            return await introspection.SupportsCapabilityAsync(capability, ct).ConfigureAwait(false);
        }

        // Fallback: capability discovery not supported
        Debug.WriteLine(
            $"Warning: Gateway does not implement IRuntimeGatewayIntrospection. " +
            $"Cannot verify capability '{capability}'.");

        return false;
    }

    /// <summary>
    /// Check if the runtime gateway supports capability discovery.
    /// </summary>
    public static bool SupportsIntrospection(this IRuntimeGateway gateway)
    {
        return gateway is IRuntimeGatewayIntrospection;
    }

    /// <summary>
    /// Get capabilities grouped by category.
    /// </summary>
    public static async Task<IReadOnlyDictionary<string, IReadOnlyList<CapabilityId>>> GetCapabilitiesByCategoryAsync(
        this IRuntimeGateway gateway,
        CancellationToken ct = default)
    {
        var capabilities = await gateway.GetCapabilitiesAsync(ct).ConfigureAwait(false);

        return capabilities
            .GroupBy(c => c.Category)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<CapabilityId>)g.ToList());
    }
}
