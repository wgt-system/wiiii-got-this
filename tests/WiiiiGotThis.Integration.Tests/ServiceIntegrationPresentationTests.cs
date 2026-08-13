using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;
using WiiiiGotThis.Presentation;

namespace WiiiiGotThis.Integration.Tests;

public sealed class ServiceIntegrationPresentationTests
{
    [Fact]
    public void Mapping_distinguishes_never_attempted_state()
    {
        var model = Map(false, false, null, null);
        Assert.Equal("Known integration — publication not refreshed yet.", model.PublicationRefreshStatusText);
    }

    [Fact]
    public void Mapping_explains_global_device_and_inherited_enablement()
    {
        var inherited = Map(false, false, null, null);
        Assert.Equal("Disabled globally", inherited.GlobalEnablementText);
        Assert.Equal("Inherit global setting", inherited.DeviceOverrideText);
        Assert.Equal("This device follows the global setting.", inherited.DeviceBehaviorText);
        Assert.Equal("Disabled on this device", inherited.EffectiveEnablementText);

        var enabled = new ServiceIntegrationPresentationViewModel(new ServiceIntegrationListItem(new("service"), "Service", true, true, true, false, false, null, null, null));
        Assert.Equal("Enabled globally", enabled.GlobalEnablementText);
        Assert.Equal("Enabled on this device", enabled.DeviceOverrideText);
        Assert.Contains("explicitly enabled", enabled.DeviceBehaviorText, StringComparison.Ordinal);
        Assert.Equal("Enabled on this device", enabled.EffectiveEnablementText);
    }

    [Fact]
    public void Mapping_distinguishes_first_failed_state()
    {
        var model = Map(false, true, IntegrationRefreshStatus.AdapterFailed, null);
        Assert.Equal("Publication refresh failed — no valid publication is available.", model.PublicationRefreshStatusText);
    }

    [Fact]
    public void Mapping_distinguishes_first_invalid_state()
    {
        var model = Map(false, true, IntegrationRefreshStatus.InvalidPublication, null);
        Assert.Equal("Provider returned an invalid publication — no valid publication is available.", model.PublicationRefreshStatusText);
    }

    [Fact]
    public void Mapping_success_does_not_claim_capability_availability()
    {
        var model = Map(true, true, IntegrationRefreshStatus.Refreshed, DateTimeOffset.UtcNow);
        Assert.Equal("Publication refresh succeeded.", model.PublicationRefreshStatusText); Assert.DoesNotContain("available", model.PublicationRefreshStatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Mapping_failed_refresh_with_snapshot_explicitly_uses_last_known_publication()
    {
        var model = Map(true, true, IntegrationRefreshStatus.AdapterFailed, DateTimeOffset.UtcNow.AddMinutes(-1));
        Assert.Contains("last-known publication", model.PublicationRefreshStatusText, StringComparison.Ordinal);
    }

    [Fact]
    public void Mapping_invalid_refresh_with_snapshot_explicitly_uses_last_known_publication()
    {
        var model = Map(true, true, IntegrationRefreshStatus.InvalidPublication, DateTimeOffset.UtcNow.AddMinutes(-1));
        Assert.Contains("last-known publication", model.PublicationRefreshStatusText, StringComparison.Ordinal);
    }

    [Fact]
    public void Publication_diagnostic_does_not_change_capability_availability_status()
    {
        var integration = Map(true, true, IntegrationRefreshStatus.AdapterFailed, DateTimeOffset.UtcNow.AddMinutes(-1));
        var service = new ServiceIdentity("service");
        var capability = new CapabilityPresentationViewModel(new CapabilityCatalogEntry(
            service, "Service", new CapabilityIdentity("capability"), "Capability", new(1, 0),
            new CapabilityResolutionResult(new("capability"), Enablement.Enabled, Availability.Available)));
        Assert.Contains("last-known publication", integration.PublicationRefreshStatusText, StringComparison.Ordinal);
        Assert.Equal("Available", capability.StatusText);
    }

    private static ServiceIntegrationPresentationViewModel Map(bool hasPublication, bool attempted, IntegrationRefreshStatus? result, DateTimeOffset? successful) =>
        new(new ServiceIntegrationListItem(new("service"), "Service", false, null, false, hasPublication, attempted, result, DateTimeOffset.UtcNow, successful));
}
