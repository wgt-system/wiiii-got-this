using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Domain.Tests;

public sealed class CoreIntegrationDomainTests
{
    [Fact] public void Empty_service_identity_is_rejected() => Assert.Throws<ArgumentException>(() => new ServiceIdentity("  "));
    [Fact] public void Empty_capability_identity_is_rejected() => Assert.Throws<ArgumentException>(() => new CapabilityIdentity(" "));
    [Fact] public void Empty_device_identity_is_rejected() => Assert.Throws<ArgumentException>(() => new DeviceIdentity(Guid.Empty));
    [Fact] public void Null_required_identities_are_rejected() {
        Assert.Throws<ArgumentNullException>(() => new ServiceIdentity(null!));
        Assert.Throws<ArgumentNullException>(() => new CapabilityIdentity(null!));
        Assert.Throws<ArgumentNullException>(() => new ServiceIntegration(null!));
        Assert.Throws<ArgumentNullException>(() => new CapabilityDescriptor(null!, new CapabilityIdentity("capability")));
        Assert.Throws<ArgumentNullException>(() => new CapabilityDescriptor(new ServiceIdentity("provider"), null!));
    }

    [Fact]
    public void Identities_are_trimmed_and_case_sensitive()
    {
        Assert.Equal(new ServiceIdentity("Provider/A"), new ServiceIdentity(" Provider/A "));
        Assert.NotEqual(new ServiceIdentity("Provider/A"), new ServiceIdentity("provider/a"));
        Assert.NotEqual(new CapabilityIdentity("capability"), new CapabilityIdentity("Capability"));
    }

    [Fact]
    public void New_integration_is_disabled_and_global_enablement_is_inherited()
    {
        var device = DeviceIdentity.New();
        var integration = new ServiceIntegration(new ServiceIdentity("provider"));
        Assert.Equal(Enablement.Disabled, integration.GetEffectiveEnablement(device));
        integration.EnableGlobally();
        Assert.Equal(Enablement.Enabled, integration.GetEffectiveEnablement(device));
    }

    [Fact]
    public void Device_override_wins_and_other_devices_are_unaffected()
    {
        var first = DeviceIdentity.New();
        var second = DeviceIdentity.New();
        var integration = EnabledIntegration();
        integration.SetDeviceOverride(first, Enablement.Disabled);
        Assert.Equal(Enablement.Disabled, integration.GetEffectiveEnablement(first));
        Assert.Equal(Enablement.Enabled, integration.GetEffectiveEnablement(second));
    }

    [Fact]
    public void Enabled_override_can_override_global_disabled_and_clear_restores_inheritance()
    {
        var device = DeviceIdentity.New();
        var integration = new ServiceIntegration(new ServiceIdentity("provider"));
        integration.SetDeviceOverride(device, Enablement.Enabled);
        Assert.Equal(Enablement.Enabled, integration.GetEffectiveEnablement(device));
        integration.ClearDeviceOverride(device);
        Assert.Equal(Enablement.Disabled, integration.GetEffectiveEnablement(device));
    }

    [Fact]
    public void Global_changes_preserve_overrides_and_same_device_set_replaces_value()
    {
        var device = DeviceIdentity.New();
        var integration = new ServiceIntegration(new ServiceIdentity("provider"));
        integration.SetDeviceOverride(device, Enablement.Enabled);
        integration.SetDeviceOverride(device, Enablement.Disabled);
        integration.EnableGlobally();
        Assert.Equal(Enablement.Disabled, integration.GetEffectiveEnablement(device));
        Assert.Single(integration.DeviceOverrides);
    }

    [Fact] public void Disabled_integration_resolves_to_disabled() => Assert.Equal(AvailabilityReason.Disabled, Resolve(new ServiceIntegration(new ServiceIdentity("provider")), new(ProviderReachability.Unreachable, ContractCompatibility.Incompatible)).Availability.Reason);

    [Theory]
    [InlineData(ProviderReachability.Reachable, ContractCompatibility.Incompatible, CurrentContextSupport.Supported, PrerequisiteState.Satisfied, PresentationInvocationSupport.Supported, AvailabilityReason.Incompatible)]
    [InlineData(ProviderReachability.Reachable, ContractCompatibility.Compatible, CurrentContextSupport.Unsupported, PrerequisiteState.Satisfied, PresentationInvocationSupport.Supported, AvailabilityReason.Unsupported)]
    [InlineData(ProviderReachability.Reachable, ContractCompatibility.Compatible, CurrentContextSupport.Supported, PrerequisiteState.Missing, PresentationInvocationSupport.Supported, AvailabilityReason.MissingPrerequisite)]
    [InlineData(ProviderReachability.Unreachable, ContractCompatibility.Compatible, CurrentContextSupport.Supported, PrerequisiteState.Satisfied, PresentationInvocationSupport.Supported, AvailabilityReason.Unreachable)]
    public void Negative_facts_produce_their_specific_reason(ProviderReachability r, ContractCompatibility c, CurrentContextSupport x, PrerequisiteState p, PresentationInvocationSupport s, AvailabilityReason expected) => Assert.Equal(expected, Resolve(EnabledIntegration(), new(r, c, x, p, s)).Availability.Reason);

    [Fact] public void Unsupported_presentation_is_unsupported() => Assert.Equal(AvailabilityReason.Unsupported, Resolve(EnabledIntegration(), new(ProviderReachability.Reachable, ContractCompatibility.Compatible, CurrentContextSupport.Supported, PrerequisiteState.Satisfied, PresentationInvocationSupport.Unsupported)).Availability.Reason);
    [Fact] public void Unknown_without_failure_is_unknown() => Assert.Equal(AvailabilityReason.Unknown, Resolve(EnabledIntegration(), new(ProviderReachability.Unknown, ContractCompatibility.Compatible, CurrentContextSupport.Supported, PrerequisiteState.Satisfied, PresentationInvocationSupport.Supported)).Availability.Reason);
    [Fact] public void All_positive_facts_are_available() => Assert.True(Resolve(EnabledIntegration(), new(ProviderReachability.Reachable, ContractCompatibility.Compatible, CurrentContextSupport.Supported, PrerequisiteState.Satisfied, PresentationInvocationSupport.Supported)).Availability.IsAvailable);

    [Fact]
    public void Resolver_uses_explicit_failure_priority()
    {
        var facts = new CapabilityResolutionFacts(ProviderReachability.Unreachable, ContractCompatibility.Incompatible, CurrentContextSupport.Unsupported, PrerequisiteState.Missing, PresentationInvocationSupport.Unsupported);
        Assert.Equal(AvailabilityReason.Incompatible, Resolve(EnabledIntegration(), facts).Availability.Reason);
        Assert.Equal(AvailabilityReason.Unsupported, Resolve(EnabledIntegration(), facts with { ContractCompatibility = ContractCompatibility.Compatible }).Availability.Reason);
        Assert.Equal(AvailabilityReason.MissingPrerequisite, Resolve(EnabledIntegration(), facts with { ContractCompatibility = ContractCompatibility.Compatible, CurrentContextSupport = CurrentContextSupport.Supported, PresentationInvocationSupport = PresentationInvocationSupport.Supported }).Availability.Reason);
    }

    [Fact]
    public void Foreign_service_descriptor_is_rejected()
    {
        var integration = EnabledIntegration();
        var descriptor = new CapabilityDescriptor(new ServiceIdentity("other"), new CapabilityIdentity("capability"));
        Assert.Throws<ArgumentException>(() => CapabilityResolver.Resolve(integration, DeviceIdentity.New(), descriptor, new()));
    }

    [Fact]
    public void Different_capabilities_can_have_different_results_in_same_context()
    {
        var integration = EnabledIntegration();
        var device = DeviceIdentity.New();
        var positive = new CapabilityResolutionFacts(ProviderReachability.Reachable, ContractCompatibility.Compatible, CurrentContextSupport.Supported, PrerequisiteState.Satisfied, PresentationInvocationSupport.Supported);
        var negative = positive with { CurrentContextSupport = CurrentContextSupport.Unsupported };
        Assert.True(CapabilityResolver.Resolve(integration, device, Descriptor("one"), positive).Availability.IsAvailable);
        Assert.Equal(AvailabilityReason.Unsupported, CapabilityResolver.Resolve(integration, device, Descriptor("two"), negative).Availability.Reason);
    }

    private static ServiceIntegration EnabledIntegration() { var integration = new ServiceIntegration(new ServiceIdentity("provider")); integration.EnableGlobally(); return integration; }
    private static CapabilityResolutionResult Resolve(ServiceIntegration integration, CapabilityResolutionFacts facts) => CapabilityResolver.Resolve(integration, DeviceIdentity.New(), Descriptor("capability"), facts);
    private static CapabilityDescriptor Descriptor(string value) => new(new ServiceIdentity("provider"), new CapabilityIdentity(value));
}
