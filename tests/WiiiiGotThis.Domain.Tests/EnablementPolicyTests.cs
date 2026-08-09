using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Domain.Tests;

public sealed class EnablementPolicyTests
{
    [Fact] public void Global_enablement_is_inherited() => Assert.Equal(Enablement.Enabled, EnablementPolicy.Effective(Enablement.Enabled, null));
    [Fact] public void Device_override_wins() => Assert.Equal(Enablement.Disabled, EnablementPolicy.Effective(Enablement.Enabled, Enablement.Disabled));
    [Fact] public void Clearing_override_restores_inheritance() => Assert.Equal(Enablement.Enabled, EnablementPolicy.Effective(Enablement.Enabled, null));
    [Fact] public void Availability_reasons_are_distinct() => Assert.NotEqual(AvailabilityReason.ProviderUnreachable, AvailabilityReason.IncompatibleContract);
}
