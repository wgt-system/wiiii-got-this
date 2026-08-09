using WiiiiGotThis.Application;
using WiiiiGotThis.Contracts;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Application.Tests;

public sealed class IntegrationPublicationReadModelTests
{
    [Fact]
    public async Task List_exposes_known_never_attempted_integration_without_publication()
    {
        var service = new ServiceIdentity("known");
        var item = await ListItem(service, new IntegrationPublicationState(service, null, PublicationRefreshObservation.NotAttempted));
        Assert.Equal("known", item.DisplayName); Assert.False(item.HasLastKnownPublication); Assert.False(item.HasRefreshBeenAttempted); Assert.Null(item.LatestRefreshResult); Assert.Null(item.LastRefreshAttemptedAtUtc); Assert.Null(item.LastSuccessfulRefreshAtUtc);
    }

    [Fact]
    public async Task List_exposes_successful_publication_state()
    {
        var service = new ServiceIdentity("successful"); var at = DateTimeOffset.UtcNow;
        var item = await ListItem(service, new IntegrationPublicationState(service, Publication(service, "Published"), PublicationRefreshObservation.Completed(at, IntegrationRefreshStatus.Refreshed, at)));
        Assert.Equal("Published", item.DisplayName); Assert.True(item.HasLastKnownPublication); Assert.True(item.HasRefreshBeenAttempted); Assert.Equal(IntegrationRefreshStatus.Refreshed, item.LatestRefreshResult); Assert.Equal(at, item.LastRefreshAttemptedAtUtc); Assert.Equal(at, item.LastSuccessfulRefreshAtUtc);
    }

    [Fact]
    public async Task List_exposes_failed_refresh_without_publication()
    {
        var service = new ServiceIdentity("failed"); var at = DateTimeOffset.UtcNow;
        var item = await ListItem(service, new IntegrationPublicationState(service, null, PublicationRefreshObservation.Completed(at, IntegrationRefreshStatus.AdapterFailed, null)));
        Assert.False(item.HasLastKnownPublication); Assert.Equal(IntegrationRefreshStatus.AdapterFailed, item.LatestRefreshResult); Assert.Equal(at, item.LastRefreshAttemptedAtUtc);
    }

    [Fact]
    public async Task List_exposes_invalid_refresh_without_publication()
    {
        var service = new ServiceIdentity("invalid"); var at = DateTimeOffset.UtcNow;
        var item = await ListItem(service, new IntegrationPublicationState(service, null, PublicationRefreshObservation.Completed(at, IntegrationRefreshStatus.InvalidPublication, null)));
        Assert.False(item.HasLastKnownPublication); Assert.Equal(IntegrationRefreshStatus.InvalidPublication, item.LatestRefreshResult); Assert.Null(item.LastSuccessfulRefreshAtUtc);
    }

    [Fact]
    public async Task List_exposes_failed_refresh_with_retained_publication()
    {
        var service = new ServiceIdentity("retained"); var successfulAt = DateTimeOffset.UtcNow.AddMinutes(-1); var failedAt = DateTimeOffset.UtcNow;
        var item = await ListItem(service, new IntegrationPublicationState(service, Publication(service, "Last known"), PublicationRefreshObservation.Completed(failedAt, IntegrationRefreshStatus.AdapterFailed, successfulAt)));
        Assert.Equal("Last known", item.DisplayName); Assert.True(item.HasLastKnownPublication); Assert.Equal(IntegrationRefreshStatus.AdapterFailed, item.LatestRefreshResult); Assert.Equal(successfulAt, item.LastSuccessfulRefreshAtUtc);
    }

    private static async Task<ServiceIntegrationListItem> ListItem(ServiceIdentity service, IntegrationPublicationState state)
    {
        var integrations = new MemoryIntegrationStore(); await integrations.SaveAsync(new ServiceIntegration(service));
        var publications = new MemoryPublicationStore(state);
        return (await new ListServiceIntegrationsUseCase(integrations, publications).ListAsync(DeviceIdentity.New())).Single();
    }

    private static ServicePublication Publication(ServiceIdentity service, string name) => new(service, name, [], DateTimeOffset.UtcNow);

    private sealed class MemoryIntegrationStore : IServiceIntegrationStore
    {
        private readonly Dictionary<ServiceIdentity, ServiceIntegration> values = [];
        public ValueTask<ServiceIntegration?> LoadAsync(ServiceIdentity id, CancellationToken cancellationToken = default) => ValueTask.FromResult(values.GetValueOrDefault(id));
        public ValueTask<IReadOnlyList<ServiceIntegration>> LoadAllAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult<IReadOnlyList<ServiceIntegration>>(values.Values.ToArray());
        public ValueTask SaveAsync(ServiceIntegration integration, CancellationToken cancellationToken = default) { values[integration.ServiceIdentity] = integration; return ValueTask.CompletedTask; }
    }

    private sealed class MemoryPublicationStore(IntegrationPublicationState state) : IIntegrationPublicationStore
    {
        public ValueTask SaveAsync(IntegrationPublicationState value, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask<IntegrationPublicationState> LoadAsync(ServiceIdentity serviceIdentity, CancellationToken cancellationToken = default) => ValueTask.FromResult(state);
    }
}
