namespace WiiiiGotThis.Domain;

public readonly record struct ServiceId(Guid Value)
{
    public static ServiceId New() => new(Guid.NewGuid());
}

public readonly record struct CapabilityId
{
    public CapabilityId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Capability identity is required.", nameof(value));
        Value = value.Trim();
    }

    public string Value { get; }
}

public readonly record struct DeviceId(Guid Value)
{
    public static DeviceId New() => new(Guid.NewGuid());
}

public enum Enablement
{
    Enabled,
    Disabled
}

public static class EnablementPolicy
{
    public static Enablement Effective(Enablement global, Enablement? deviceOverride) => deviceOverride ?? global;
}

public enum AvailabilityReason
{
    Available,
    IntegrationDisabled,
    ProviderUnreachable,
    IncompatibleContract,
    UnsupportedContext,
    MissingPrerequisite
}

public readonly record struct Availability(AvailabilityReason Reason, string? Detail = null)
{
    public bool IsAvailable => Reason == AvailabilityReason.Available;
}
