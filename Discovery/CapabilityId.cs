namespace Keon.Sdk.Discovery;

/// <summary>
/// Strongly-typed capability identifier.
/// Format: category.operation (e.g., "memory.search", "runtime.execute")
/// </summary>
public readonly record struct CapabilityId(string Value)
{
    public override string ToString() => Value;

    public static CapabilityId From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("CapabilityId cannot be null or empty.", nameof(value));

        return new CapabilityId(value);
    }

    public string Category => Value.Contains('.') ? Value.Split('.')[0] : Value;
    public string Operation => Value.Contains('.') ? Value.Split('.')[1] : string.Empty;
}
