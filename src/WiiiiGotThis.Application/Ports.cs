using WiiiiGotThis.Contracts;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Application;

public interface IIntegrationPublicationStore
{
    ValueTask SaveAsync(ServicePublication publication, CancellationToken cancellationToken = default);
    ValueTask<ServicePublication?> LoadAsync(ServiceIdentity serviceIdentity, CancellationToken cancellationToken = default);
}

public sealed class LocalDeviceConfiguration
{
    public LocalDeviceConfiguration(DeviceIdentity deviceIdentity, string displayName)
    {
        ArgumentNullException.ThrowIfNull(deviceIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        DeviceIdentity = deviceIdentity;
        DisplayName = displayName.Trim();
    }

    public DeviceIdentity DeviceIdentity { get; }
    public string DisplayName { get; }
}

public interface ILocalDeviceStore
{
    ValueTask<LocalDeviceConfiguration?> LoadAsync(CancellationToken cancellationToken = default);
    ValueTask SaveAsync(LocalDeviceConfiguration configuration, CancellationToken cancellationToken = default);
}

public interface IServiceIntegrationStore
{
    ValueTask<ServiceIntegration?> LoadAsync(ServiceIdentity serviceIdentity, CancellationToken cancellationToken = default);
    ValueTask<IReadOnlyList<ServiceIntegration>> LoadAllAsync(CancellationToken cancellationToken = default);
    ValueTask SaveAsync(ServiceIntegration integration, CancellationToken cancellationToken = default);
}

public interface IIntegrationAdapterCatalog
{
    IReadOnlyList<IIntegrationAdapter> Adapters { get; }
}

public sealed class StaticIntegrationAdapterCatalog : IIntegrationAdapterCatalog
{
    public StaticIntegrationAdapterCatalog(IEnumerable<IIntegrationAdapter> adapters)
    {
        ArgumentNullException.ThrowIfNull(adapters);
        var registered = adapters.ToArray();
        for (var index = 0; index < registered.Length; index++)
        {
            ArgumentNullException.ThrowIfNull(registered[index], $"adapters[{index}]");
            if (registered.Take(index).Any(existing => existing.ServiceId == registered[index].ServiceId))
                throw new ArgumentException($"An adapter for Service '{registered[index].ServiceId.Value}' is already registered.", nameof(adapters));
        }
        Adapters = Array.AsReadOnly(registered);
    }

    public IReadOnlyList<IIntegrationAdapter> Adapters { get; }
}

public enum IntegrationRefreshStatus { Refreshed, AdapterFailed, InvalidPublication }

public sealed record IntegrationRefreshResult(ServiceIdentity ServiceIdentity, IntegrationRefreshStatus Status);

public sealed class RefreshPublicationsUseCase(IIntegrationAdapterCatalog adapters, IServiceIntegrationStore integrations, IIntegrationPublicationStore publications)
{
    public async ValueTask<IReadOnlyList<IntegrationRefreshResult>> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<IntegrationRefreshResult>(adapters.Adapters.Count);
        foreach (var adapter in adapters.Adapters)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var integration = await integrations.LoadAsync(adapter.ServiceId, cancellationToken);
            if (integration is null)
                await integrations.SaveAsync(new ServiceIntegration(adapter.ServiceId), cancellationToken);

            ServicePublication publication;
            try
            {
                publication = await adapter.GetPublicationAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                results.Add(new(adapter.ServiceId, IntegrationRefreshStatus.AdapterFailed));
                continue;
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                results.Add(new(adapter.ServiceId, IntegrationRefreshStatus.AdapterFailed));
                continue;
            }

            if (!PublicationValidator.IsValid(adapter, publication))
            {
                results.Add(new(adapter.ServiceId, IntegrationRefreshStatus.InvalidPublication));
                continue;
            }

            await publications.SaveAsync(publication, cancellationToken);
            results.Add(new(adapter.ServiceId, IntegrationRefreshStatus.Refreshed));
        }
        return results;
    }
}

internal static class PublicationValidator
{
    public static bool IsValid(IIntegrationAdapter adapter, ServicePublication? publication)
    {
        if (publication is null || publication.ServiceId != adapter.ServiceId || string.IsNullOrWhiteSpace(publication.DisplayName) || publication.Capabilities is null) return false;
        var identities = new HashSet<CapabilityIdentity>();
        foreach (var capability in publication.Capabilities)
        {
            if (capability is null || string.IsNullOrWhiteSpace(capability.Title) || capability.ContractVersion is null || capability.Id is null || !identities.Add(capability.Id)) return false;
        }
        return true;
    }
}

public sealed record CapabilityCatalogEntry(
    ServiceIdentity ServiceIdentity,
    string ServiceDisplayName,
    CapabilityIdentity CapabilityIdentity,
    string CapabilityTitle,
    Version ContractVersion,
    CapabilityResolutionResult Resolution);

public sealed class ResolveCapabilityCatalogUseCase(
    IIntegrationAdapterCatalog adapters,
    IServiceIntegrationStore integrations,
    IIntegrationPublicationStore publications)
{
    public async ValueTask<IReadOnlyList<CapabilityCatalogEntry>> ResolveAsync(DeviceIdentity currentDevice, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currentDevice);
        var result = new List<CapabilityCatalogEntry>();
        foreach (var adapter in adapters.Adapters)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var integration = await integrations.LoadAsync(adapter.ServiceId, cancellationToken);
            var publication = await publications.LoadAsync(adapter.ServiceId, cancellationToken);
            if (integration is null || publication is null) continue;
            foreach (var capability in publication.Capabilities)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CapabilityResolutionFacts facts;
                if (integration.GetEffectiveEnablement(currentDevice) == Enablement.Disabled)
                    facts = new();
                else
                {
                    try { facts = await adapter.ObserveCapabilityAsync(capability, cancellationToken); }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { facts = new(); }
                    catch (Exception) when (!cancellationToken.IsCancellationRequested) { facts = new(); }
                }
                var descriptor = new CapabilityDescriptor(publication.ServiceId, capability.Id);
                var resolution = CapabilityResolver.Resolve(integration, currentDevice, descriptor, facts);
                result.Add(new(adapter.ServiceId, publication.DisplayName, capability.Id, capability.Title, capability.ContractVersion, resolution));
            }
        }
        return result;
    }
}
