using WiiiiGotThis.Contracts;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Application;

public interface IIntegrationPublicationStore
{
    ValueTask SaveAsync(ServicePublication publication, CancellationToken cancellationToken = default);
    ValueTask<ServicePublication?> LoadAsync(ServiceId serviceId, CancellationToken cancellationToken = default);
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
