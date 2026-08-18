using WiiiiGotThis.Contracts;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Desktop;

internal sealed class OrientationDesktopIntegrationAdapter : IIntegrationAdapter
{
    private static readonly ServiceIdentity OrientationServiceId = new("orientation");

    public ServiceIdentity ServiceId => OrientationServiceId;

    public ValueTask<ServicePublication> GetPublicationAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new ServicePublication(
            OrientationServiceId,
            "Orientation",
            Array.Empty<CapabilityPublication>(),
            DateTimeOffset.UtcNow));
    }

    public ValueTask<CapabilityResolutionFacts> ObserveCapabilityAsync(
        CapabilityPublication capability,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(capability);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new CapabilityResolutionFacts(
            ProviderReachability.Reachable,
            ContractCompatibility.Incompatible,
            CurrentContextSupport.Unsupported,
            PrerequisiteState.Satisfied,
            PresentationInvocationSupport.Unsupported));
    }
}
