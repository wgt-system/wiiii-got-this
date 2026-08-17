using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Presentation;

public sealed class CapabilityPresentationViewModel(CapabilityCatalogEntry entry)
{
    public ServiceIdentity ServiceIdentity => entry.ServiceIdentity;
    public string ServiceDisplayName => entry.ServiceDisplayName;
    public CapabilityIdentity CapabilityIdentity => entry.CapabilityIdentity;
    public string CapabilityTitle => entry.CapabilityTitle;
    public Version ContractVersion => entry.ContractVersion;
    public bool IsAvailable => entry.Resolution.Availability.IsAvailable;
    public string StatusText => IsAvailable ? "Available" : entry.Resolution.Availability.Reason switch
    {
        AvailabilityReason.Disabled => "Integration is disabled on this device.",
        AvailabilityReason.Unknown => "Current availability could not be determined.",
        AvailabilityReason.Unreachable => "Provider is currently unavailable.",
        AvailabilityReason.Incompatible => "This capability version is not supported.",
        AvailabilityReason.Unsupported => "This capability is not supported in the current client context.",
        AvailabilityReason.MissingPrerequisite => "A required prerequisite is missing.",
        _ => "Current availability could not be determined."
    };
    public bool CanOpen => IsAvailable && (
        string.Equals(CapabilityIdentity.Value, "reference.available", StringComparison.Ordinal) ||
        string.Equals(CapabilityIdentity.Value, "vocation.opportunity_overview", StringComparison.Ordinal) ||
        string.Equals(CapabilityIdentity.Value, "vocation.map_projection", StringComparison.Ordinal));
}
