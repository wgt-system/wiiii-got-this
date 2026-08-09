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

public sealed class RefreshPublicationsUseCase(IIntegrationAdapterCatalog adapters, IIntegrationPublicationStore store)
{
    public async ValueTask RefreshAsync(CancellationToken cancellationToken = default)
    {
        foreach (var adapter in adapters.Adapters)
        {
            var publication = await adapter.GetPublicationAsync(cancellationToken);
            await store.SaveAsync(publication, cancellationToken);
        }
    }
}
