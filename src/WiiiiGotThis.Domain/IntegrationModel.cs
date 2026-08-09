using System.Collections.ObjectModel;

namespace WiiiiGotThis.Domain;

public sealed class ServiceIdentity : IEquatable<ServiceIdentity>
{
    public ServiceIdentity(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Service identity is required.", nameof(value));
        Value = value.Trim();
    }
    public string Value { get; }
    public bool Equals(ServiceIdentity? other) => other is not null && StringComparer.Ordinal.Equals(Value, other.Value);
    public override bool Equals(object? obj) => obj is ServiceIdentity other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public static bool operator ==(ServiceIdentity? left, ServiceIdentity? right) => left is null ? right is null : left.Equals(right);
    public static bool operator !=(ServiceIdentity? left, ServiceIdentity? right) => !(left == right);
}

public sealed class CapabilityIdentity : IEquatable<CapabilityIdentity>
{
    public CapabilityIdentity(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Capability identity is required.", nameof(value));
        Value = value.Trim();
    }
    public string Value { get; }
    public bool Equals(CapabilityIdentity? other) => other is not null && StringComparer.Ordinal.Equals(Value, other.Value);
    public override bool Equals(object? obj) => obj is CapabilityIdentity other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public static bool operator ==(CapabilityIdentity? left, CapabilityIdentity? right) => left is null ? right is null : left.Equals(right);
    public static bool operator !=(CapabilityIdentity? left, CapabilityIdentity? right) => !(left == right);
}

public sealed class DeviceIdentity : IEquatable<DeviceIdentity>
{
    public DeviceIdentity(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("Device identity cannot be empty.", nameof(value));
        Value = value;
    }
    public Guid Value { get; }
    public static DeviceIdentity New() => new(Guid.NewGuid());
    public bool Equals(DeviceIdentity? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is DeviceIdentity other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(DeviceIdentity? left, DeviceIdentity? right) => left is null ? right is null : left.Equals(right);
    public static bool operator !=(DeviceIdentity? left, DeviceIdentity? right) => !(left == right);
}

public enum Enablement { Enabled, Disabled }

public sealed class ServiceIntegration
{
    private readonly Dictionary<DeviceIdentity, Enablement> deviceOverrides = [];

    public ServiceIntegration(ServiceIdentity serviceIdentity)
    {
        ArgumentNullException.ThrowIfNull(serviceIdentity);
        ServiceIdentity = serviceIdentity;
        GlobalEnablement = Enablement.Disabled;
    }

    public ServiceIdentity ServiceIdentity { get; }
    public Enablement GlobalEnablement { get; private set; }
    public IReadOnlyDictionary<DeviceIdentity, Enablement> DeviceOverrides => new ReadOnlyDictionary<DeviceIdentity, Enablement>(new Dictionary<DeviceIdentity, Enablement>(deviceOverrides));
    public void EnableGlobally() => GlobalEnablement = Enablement.Enabled;
    public void DisableGlobally() => GlobalEnablement = Enablement.Disabled;
    public void SetDeviceOverride(DeviceIdentity device, Enablement enablement) => deviceOverrides[device] = enablement;
    public void ClearDeviceOverride(DeviceIdentity device) => deviceOverrides.Remove(device);
    public Enablement GetEffectiveEnablement(DeviceIdentity device) => deviceOverrides.TryGetValue(device, out var value) ? value : GlobalEnablement;
}

public sealed class CapabilityDescriptor
{
    public CapabilityDescriptor(ServiceIdentity serviceIdentity, CapabilityIdentity capabilityIdentity)
    {
        ArgumentNullException.ThrowIfNull(serviceIdentity);
        ArgumentNullException.ThrowIfNull(capabilityIdentity);
        ServiceIdentity = serviceIdentity;
        CapabilityIdentity = capabilityIdentity;
    }

    public ServiceIdentity ServiceIdentity { get; }
    public CapabilityIdentity CapabilityIdentity { get; }
}

public enum ProviderReachability { Unknown, Reachable, Unreachable }
public enum ContractCompatibility { Unknown, Compatible, Incompatible }
public enum CurrentContextSupport { Unknown, Supported, Unsupported }
public enum PrerequisiteState { Unknown, Satisfied, Missing }
public enum PresentationInvocationSupport { Unknown, Supported, Unsupported }

public sealed record CapabilityResolutionFacts(
    ProviderReachability ProviderReachability = ProviderReachability.Unknown,
    ContractCompatibility ContractCompatibility = ContractCompatibility.Unknown,
    CurrentContextSupport CurrentContextSupport = CurrentContextSupport.Unknown,
    PrerequisiteState PrerequisiteState = PrerequisiteState.Unknown,
    PresentationInvocationSupport PresentationInvocationSupport = PresentationInvocationSupport.Unknown);

public enum AvailabilityReason { Disabled, Unknown, Unreachable, Incompatible, Unsupported, MissingPrerequisite }

public sealed class Availability : IEquatable<Availability>
{
    private Availability(bool isAvailable, AvailabilityReason? reason)
    {
        if (isAvailable && reason is not null) throw new ArgumentException("Available cannot have an unavailable reason.", nameof(reason));
        if (!isAvailable && reason is null) throw new ArgumentException("Unavailable must have an unavailable reason.", nameof(reason));
        IsAvailable = isAvailable;
        Reason = reason;
    }
    public bool IsAvailable { get; }
    public AvailabilityReason? Reason { get; }
    public static Availability Available { get; } = new(true, null);
    public static Availability Unavailable(AvailabilityReason reason) => new(false, reason);
    public bool Equals(Availability? other) => other is not null && IsAvailable == other.IsAvailable && Reason == other.Reason;
    public override bool Equals(object? obj) => obj is Availability other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(IsAvailable, Reason);
}

public readonly record struct CapabilityResolutionResult(CapabilityIdentity CapabilityIdentity, Enablement EffectiveEnablement, Availability Availability);

public static class CapabilityResolver
{
    public static CapabilityResolutionResult Resolve(ServiceIntegration integration, DeviceIdentity currentDevice, CapabilityDescriptor descriptor, CapabilityResolutionFacts facts)
    {
        ArgumentNullException.ThrowIfNull(integration);
        if (descriptor.ServiceIdentity != integration.ServiceIdentity) throw new ArgumentException("Capability descriptor belongs to another service.", nameof(descriptor));
        var effective = integration.GetEffectiveEnablement(currentDevice);
        if (effective == Enablement.Disabled) return new(descriptor.CapabilityIdentity, effective, Availability.Unavailable(AvailabilityReason.Disabled));
        var reason = SelectNegativeReason(facts);
        if (reason is not null) return new(descriptor.CapabilityIdentity, effective, Availability.Unavailable(reason.Value));
        return new(descriptor.CapabilityIdentity, effective, HasUnknown(facts) ? Availability.Unavailable(AvailabilityReason.Unknown) : Availability.Available);
    }

    private static AvailabilityReason? SelectNegativeReason(CapabilityResolutionFacts facts)
    {
        if (facts.ContractCompatibility == ContractCompatibility.Incompatible) return AvailabilityReason.Incompatible;
        if (facts.CurrentContextSupport == CurrentContextSupport.Unsupported || facts.PresentationInvocationSupport == PresentationInvocationSupport.Unsupported) return AvailabilityReason.Unsupported;
        if (facts.PrerequisiteState == PrerequisiteState.Missing) return AvailabilityReason.MissingPrerequisite;
        if (facts.ProviderReachability == ProviderReachability.Unreachable) return AvailabilityReason.Unreachable;
        return null;
    }

    private static bool HasUnknown(CapabilityResolutionFacts facts) => facts.ProviderReachability == ProviderReachability.Unknown || facts.ContractCompatibility == ContractCompatibility.Unknown || facts.CurrentContextSupport == CurrentContextSupport.Unknown || facts.PrerequisiteState == PrerequisiteState.Unknown || facts.PresentationInvocationSupport == PresentationInvocationSupport.Unknown;
}
