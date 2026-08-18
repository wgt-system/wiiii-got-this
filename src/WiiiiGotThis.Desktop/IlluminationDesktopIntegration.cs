using Avalonia.Controls;
using Illumination.Desktop;
using WiiiiGotThis.Contracts;
using WiiiiGotThis.Domain;
using WiiiiGotThis.Presentation;

namespace WiiiiGotThis.Desktop;

internal sealed class IlluminationDesktopProductSurfaceSource : IIlluminationProductSurfaceSource
{
    public Task<Control> CreateAsync() => IlluminationProductSurfaceFactory.CreateAsync();
}

internal sealed class IlluminationDesktopIntegrationAdapter : IIntegrationAdapter
{
    private static readonly ServiceIdentity IlluminationServiceId = new("illumination");

    public ServiceIdentity ServiceId => IlluminationServiceId;

    public ValueTask<ServicePublication> GetPublicationAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new ServicePublication(
            IlluminationServiceId,
            "Illumination",
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
