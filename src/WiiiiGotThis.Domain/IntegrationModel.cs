using System.Collections.ObjectModel;

namespace WiiiiGotThis.Domain;

public readonly record struct ServiceIdentity
{
    public ServiceIdentity(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Service identity is required.", nameof(value));
        Value = value.Trim();
    }
    public string Value { get; }
}

public readonly record struct CapabilityIdentity
{
    public CapabilityIdentity(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Capability identity is required.", nameof(value));
        Value = value.Trim();
    }
    public string Value { get; }
}

public readonly record struct DeviceIdentity
{
    public DeviceIdentity(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("Device identity cannot be empty.", nameof(value));
        Value = value;
    }
    public Guid Value { get; }
    public static DeviceIdentity New() => new(Guid.NewGuid());
}

public enum Enablement { Enabled, Disabled }

public sealed class ServiceIntegration
{
    private readonly Dictionary<DeviceIdentity, Enablement> deviceOverrides = [];

    public ServiceIntegration(ServiceIdentity serviceIdentity)
    {
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

public readonly record struct CapabilityDescriptor(ServiceIdentity ServiceIdentity, CapabilityIdentity CapabilityIdentity);

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

public readonly record struct Availability
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
    public static Availability Available => new(true, null);
    public static Availability Unavailable(AvailabilityReason reason) => new(false, reason);
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
