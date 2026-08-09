using WiiiiGotThis.Application;
using WiiiiGotThis.Contracts;
using WiiiiGotThis.Integrations.Reference;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Application.Tests;

public sealed class ReferenceIntegrationTests
{
    [Fact]
    public void Local_device_configuration_validates_and_trims_only_its_display_name()
    {
        var identity = DeviceIdentity.New();
        var configuration = new LocalDeviceConfiguration(identity, "  Windows PC  ");
        Assert.Equal(identity, configuration.DeviceIdentity);
        Assert.Equal("Windows PC", configuration.DisplayName);
        Assert.Throws<ArgumentException>(() => new LocalDeviceConfiguration(identity, " \t "));
        Assert.Throws<ArgumentNullException>(() => new LocalDeviceConfiguration(null!, "name"));
    }

    [Fact]
    public async Task Reference_publication_has_stable_identity_and_distinct_states()
    {
        var publication = await new ReferenceIntegrationAdapter().GetPublicationAsync();
        Assert.Equal(ReferenceIntegrationAdapter.StableServiceIdentity, publication.ServiceId);
        Assert.Equal(3, publication.Capabilities.Count);
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
        public ValueTask<ServicePublication?> LoadAsync(ServiceIdentity serviceIdentity, CancellationToken cancellationToken = default) => ValueTask.FromResult<ServicePublication?>(null);
    }
}
