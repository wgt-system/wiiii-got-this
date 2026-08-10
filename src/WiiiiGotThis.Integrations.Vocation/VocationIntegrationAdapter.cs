using WiiiiGotThis.Application;
using WiiiiGotThis.Contracts;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Integrations.Vocation;

public sealed class VocationIntegrationAdapter(
    IVocationOpportunityOverviewSource source,
    TimeProvider? timeProvider = null) : IIntegrationAdapter
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;

    public ServiceIdentity ServiceId => VocationIntegrationMetadata.ServiceId;

    public async ValueTask<ServicePublication> GetPublicationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await source.GetAsync(cancellationToken);
        }
        catch (VocationOpportunityOverviewSourceException exception) when (
            !cancellationToken.IsCancellationRequested &&
            exception.Kind == VocationOpportunityOverviewSourceFailureKind.IncompatibleContract)
        {
            if (!Version.TryParse(exception.ObservedContractVersion, out var observedVersion))
                throw new VocationOpportunityOverviewSourceException(
                    VocationOpportunityOverviewSourceFailureKind.InvalidContract,
                    "The Vocation contract version cannot be represented.");
            return Publication(observedVersion);
        }

        return Publication(VocationIntegrationMetadata.OpportunityOverviewContractVersion);
    }

    public async ValueTask<CapabilityResolutionFacts> ObserveCapabilityAsync(
        CapabilityPublication capability,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(capability);
        if (capability.Id != VocationIntegrationMetadata.OpportunityOverviewCapability ||
            capability.ContractVersion != VocationIntegrationMetadata.OpportunityOverviewContractVersion)
            return IncompatibleFacts();

        try
        {
            await source.GetAsync(cancellationToken);
            return AvailableFacts();
        }
        catch (VocationOpportunityOverviewSourceException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return exception.Kind switch
            {
                VocationOpportunityOverviewSourceFailureKind.Unavailable => new(ProviderReachability.Unreachable, ContractCompatibility.Compatible, CurrentContextSupport.Supported, PrerequisiteState.Satisfied, PresentationInvocationSupport.Supported),
                VocationOpportunityOverviewSourceFailureKind.IncompatibleContract => IncompatibleFacts(),
                _ => new()
            };
        }
    }

    private ServicePublication Publication(Version version) => new(
        ServiceId,
        VocationIntegrationMetadata.ServiceDisplayName,
        [new CapabilityPublication(
            VocationIntegrationMetadata.OpportunityOverviewCapability,
            VocationIntegrationMetadata.OpportunityOverviewTitle,
            version)],
        clock.GetUtcNow());

    private static CapabilityResolutionFacts AvailableFacts() => new(
        ProviderReachability.Reachable,
        ContractCompatibility.Compatible,
        CurrentContextSupport.Supported,
        PrerequisiteState.Satisfied,
        PresentationInvocationSupport.Supported);

    private static CapabilityResolutionFacts IncompatibleFacts() => new(
        ProviderReachability.Reachable,
        ContractCompatibility.Incompatible,
        CurrentContextSupport.Supported,
        PrerequisiteState.Satisfied,
        PresentationInvocationSupport.Supported);
}
