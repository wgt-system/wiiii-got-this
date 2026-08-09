using WiiiiGotThis.Application;
using WiiiiGotThis.Contracts;
using WiiiiGotThis.Integrations.Reference;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Application.Tests;

public sealed class ReferenceIntegrationTests
{
    [Fact]
    public async Task Reference_publication_has_stable_identity_and_distinct_states()
    {
        var publication = await new ReferenceIntegrationAdapter().GetPublicationAsync();
        Assert.Equal(ReferenceIntegrationAdapter.StableServiceId, publication.ServiceId);
        Assert.Contains(publication.Capabilities, item => item.Availability.Reason == AvailabilityReason.Available);
        Assert.Contains(publication.Capabilities, item => item.Availability.Reason == AvailabilityReason.UnsupportedContext);
        Assert.Contains(publication.Capabilities, item => item.Availability.Reason == AvailabilityReason.ProviderUnreachable);
    }

    [Fact]
    public async Task Refresh_use_case_calls_each_static_adapter()
    {
        var adapter = new ReferenceIntegrationAdapter();
        var store = new RecordingStore();
        await new RefreshPublicationsUseCase(new StaticAdapters(adapter), store).RefreshAsync();
        Assert.Single(store.Publications);
    }

    private sealed class StaticAdapters(params IIntegrationAdapter[] adapters) : IIntegrationAdapterCatalog
    { public IReadOnlyList<IIntegrationAdapter> Adapters { get; } = adapters; }
    private sealed class RecordingStore : IIntegrationPublicationStore
    {
        public List<ServicePublication> Publications { get; } = [];
        public ValueTask SaveAsync(ServicePublication publication, CancellationToken cancellationToken = default) { Publications.Add(publication); return ValueTask.CompletedTask; }
        public ValueTask<ServicePublication?> LoadAsync(ServiceId serviceId, CancellationToken cancellationToken = default) => ValueTask.FromResult<ServicePublication?>(null);
    }
}
