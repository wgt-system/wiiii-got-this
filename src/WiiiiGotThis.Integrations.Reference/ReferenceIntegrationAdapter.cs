using WiiiiGotThis.Contracts;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Integrations.Reference;

public sealed class ReferenceIntegrationAdapter : IIntegrationAdapter
{
    public static readonly ServiceIdentity StableServiceIdentity = new("reference-service");
    public ServiceIdentity ServiceId => StableServiceIdentity;

    public ValueTask<ServicePublication> GetPublicationAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new ServicePublication(
            StableServiceIdentity,
            "Reference Integration",
            [
                new(new CapabilityIdentity("reference.available"), "Reference capability", new Version(1, 0)),
                new(new CapabilityIdentity("reference.unsupported"), "Unsupported capability", new Version(1, 0)),
                new(new CapabilityIdentity("reference.unavailable"), "Unavailable capability", new Version(1, 0)),
                new(new CapabilityIdentity("reference.version-mismatch"), "Version mismatch capability", new Version(99, 0))
            ],
            DateTimeOffset.UtcNow));
    }

    public ValueTask<CapabilityResolutionFacts> ObserveCapabilityAsync(CapabilityPublication capability, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var compatibility = capability.ContractVersion.Major == 1 ? ContractCompatibility.Compatible : ContractCompatibility.Incompatible;
        var facts = capability.Id.Value switch
        {
            "reference.unsupported" => new CapabilityResolutionFacts(ProviderReachability.Reachable, compatibility, CurrentContextSupport.Unsupported, PrerequisiteState.Satisfied, PresentationInvocationSupport.Supported),
            "reference.unavailable" => new CapabilityResolutionFacts(ProviderReachability.Unreachable, compatibility, CurrentContextSupport.Supported, PrerequisiteState.Satisfied, PresentationInvocationSupport.Supported),
            _ => new CapabilityResolutionFacts(ProviderReachability.Reachable, compatibility, CurrentContextSupport.Supported, PrerequisiteState.Satisfied, PresentationInvocationSupport.Supported)
        };
        return ValueTask.FromResult(facts);
    }
}
