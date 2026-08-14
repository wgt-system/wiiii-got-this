using WiiiiGotThis.Application;
using WiiiiGotThis.Contracts;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Integrations.Vocation;

public sealed class VocationIntegrationAdapter : IIntegrationAdapter
{
    private readonly IVocationOpportunityOverviewSource overviewSource;
    private readonly IVocationMapProjectionSource mapSource;
    private readonly TimeProvider clock;

    public VocationIntegrationAdapter(
        IVocationOpportunityOverviewSource overviewSource,
        TimeProvider? timeProvider = null)
        : this(overviewSource, new UnavailableMapProjectionSource(), timeProvider)
    {
    }

    public VocationIntegrationAdapter(
        IVocationOpportunityOverviewSource overviewSource,
        IVocationMapProjectionSource mapSource,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(overviewSource);
        ArgumentNullException.ThrowIfNull(mapSource);
        this.overviewSource = overviewSource;
        this.mapSource = mapSource;
        clock = timeProvider ?? TimeProvider.System;
    }

    public ServiceIdentity ServiceId => VocationIntegrationMetadata.ServiceId;

    public async ValueTask<ServicePublication> GetPublicationAsync(CancellationToken cancellationToken = default)
    {
        var overviewVersion = await PublishedOverviewVersionAsync(cancellationToken);
        var mapVersion = await PublishedMapVersionAsync(cancellationToken);
        return Publication(overviewVersion, mapVersion);
    }

    public async ValueTask<CapabilityResolutionFacts> ObserveCapabilityAsync(
        CapabilityPublication capability,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(capability);
        if (capability.Id == VocationIntegrationMetadata.OpportunityOverviewCapability)
        {
            if (capability.ContractVersion != VocationIntegrationMetadata.OpportunityOverviewContractVersion)
                return IncompatibleFacts();
            return await ObserveOverviewAsync(cancellationToken);
        }

        if (capability.Id == VocationIntegrationMetadata.MapProjectionCapability)
        {
            if (capability.ContractVersion != VocationIntegrationMetadata.MapProjectionContractVersion)
                return IncompatibleFacts();
            return await ObserveMapAsync(cancellationToken);
        }

        return IncompatibleFacts();
    }

    private async ValueTask<Version> PublishedOverviewVersionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await overviewSource.GetAsync(cancellationToken);
            return VocationIntegrationMetadata.OpportunityOverviewContractVersion;
        }
        catch (VocationOpportunityOverviewSourceException exception) when (
            !cancellationToken.IsCancellationRequested &&
            exception.Kind == VocationOpportunityOverviewSourceFailureKind.IncompatibleContract)
        {
            return ParseObservedOverviewVersion(exception.ObservedContractVersion);
        }
        catch (VocationOpportunityOverviewSourceException) when (!cancellationToken.IsCancellationRequested)
        {
            return VocationIntegrationMetadata.OpportunityOverviewContractVersion;
        }
    }

    private async ValueTask<Version> PublishedMapVersionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await mapSource.GetAsync(cancellationToken);
            return VocationIntegrationMetadata.MapProjectionContractVersion;
        }
        catch (VocationMapProjectionSourceException exception) when (
            !cancellationToken.IsCancellationRequested &&
            exception.Kind == VocationMapProjectionSourceFailureKind.IncompatibleContract)
        {
            return ParseObservedMapVersion(exception.ObservedContractVersion);
        }
        catch (VocationMapProjectionSourceException) when (!cancellationToken.IsCancellationRequested)
        {
            return VocationIntegrationMetadata.MapProjectionContractVersion;
        }
    }

    private async ValueTask<CapabilityResolutionFacts> ObserveOverviewAsync(CancellationToken cancellationToken)
    {
        try
        {
            await overviewSource.GetAsync(cancellationToken);
            return AvailableFacts();
        }
        catch (VocationOpportunityOverviewSourceException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return exception.Kind switch
            {
                VocationOpportunityOverviewSourceFailureKind.Unavailable => UnreachableFacts(),
                VocationOpportunityOverviewSourceFailureKind.IncompatibleContract => IncompatibleFacts(),
                _ => new()
            };
        }
    }

    private async ValueTask<CapabilityResolutionFacts> ObserveMapAsync(CancellationToken cancellationToken)
    {
        try
        {
            await mapSource.GetAsync(cancellationToken);
            return AvailableFacts();
        }
        catch (VocationMapProjectionSourceException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return exception.Kind switch
            {
                VocationMapProjectionSourceFailureKind.Unavailable => UnreachableFacts(),
                VocationMapProjectionSourceFailureKind.IncompatibleContract => IncompatibleFacts(),
                _ => new()
            };
        }
    }

    private ServicePublication Publication(Version overviewVersion, Version mapVersion) => new(
        ServiceId,
        VocationIntegrationMetadata.ServiceDisplayName,
        [
            new CapabilityPublication(
                VocationIntegrationMetadata.OpportunityOverviewCapability,
                VocationIntegrationMetadata.OpportunityOverviewTitle,
                overviewVersion),
            new CapabilityPublication(
                VocationIntegrationMetadata.MapProjectionCapability,
                VocationIntegrationMetadata.MapProjectionTitle,
                mapVersion)
        ],
        clock.GetUtcNow());

    private static Version ParseObservedOverviewVersion(string? observedVersion)
    {
        if (Version.TryParse(observedVersion, out var parsed)) return parsed;
        throw new VocationOpportunityOverviewSourceException(
            VocationOpportunityOverviewSourceFailureKind.InvalidContract,
            "The Vocation contract version cannot be represented.");
    }

    private static Version ParseObservedMapVersion(string? observedVersion)
    {
        if (Version.TryParse(observedVersion, out var parsed)) return parsed;
        throw new VocationMapProjectionSourceException(
            VocationMapProjectionSourceFailureKind.InvalidContract,
            "The Vocation map contract version cannot be represented.");
    }

    private static CapabilityResolutionFacts AvailableFacts() => new(
        ProviderReachability.Reachable,
        ContractCompatibility.Compatible,
        CurrentContextSupport.Supported,
        PrerequisiteState.Satisfied,
        PresentationInvocationSupport.Supported);

    private static CapabilityResolutionFacts UnreachableFacts() => new(
        ProviderReachability.Unreachable,
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

    private sealed class UnavailableMapProjectionSource : IVocationMapProjectionSource
    {
        public ValueTask<VocationMapProjection> GetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new VocationMapProjectionSourceException(
                VocationMapProjectionSourceFailureKind.Unavailable,
                "The Vocation map projection source is not composed.");
        }
    }
}
