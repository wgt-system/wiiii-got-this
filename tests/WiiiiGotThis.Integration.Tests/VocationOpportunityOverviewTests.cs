using System.Numerics;
using System.Globalization;
using WiiiiGotThis.Application;
using WiiiiGotThis.Contracts;
using WiiiiGotThis.Domain;
using WiiiiGotThis.Integrations.Vocation;
using WiiiiGotThis.Presentation;

namespace WiiiiGotThis.Integration.Tests;

public sealed class VocationOpportunityOverviewTests
{
    [Fact]
    public async Task Loaded_snapshot_maps_published_fields_in_provider_order()
    {
        var source = new FakeSource(Snapshot(
            new VocationOpportunity("one", "Junior Software Developer", new("company", "Company GmbH"), [new("Hamburg", "Hamburg", null, "DE", "city")], BigInteger.One),
            new VocationOpportunity("two", "Platform Engineer", new("company2", "Another GmbH"), [new("Berlin", "Berlin", null, "DE", "city"), new("Remote office", null, null, null, "unknown")], BigInteger.Parse("123456789012345678901234567890", CultureInfo.InvariantCulture)),
            new VocationOpportunity("three", "Researcher", new("company3", "Research GmbH"), [], BigInteger.Zero)));
        var viewModel = new VocationOpportunityOverviewViewModel(new GetVocationOpportunityOverviewUseCase(source));
        await viewModel.RefreshAsync();
        Assert.Equal(VocationOpportunityOverviewPresentationState.Loaded, viewModel.State);
        Assert.Equal(["Junior Software Developer", "Platform Engineer", "Researcher"], viewModel.Opportunities.Select(x => x.Title));
        Assert.Equal("Company GmbH", viewModel.Opportunities[0].CompanyName); Assert.Equal("Hamburg", viewModel.Opportunities[0].WorkLocationsText); Assert.Equal("1 posting", viewModel.Opportunities[0].PostingCountText);
        Assert.Equal("Berlin · Remote office", viewModel.Opportunities[1].WorkLocationsText); Assert.Equal("123456789012345678901234567890 postings", viewModel.Opportunities[1].PostingCountText); Assert.Equal("No location published", viewModel.Opportunities[2].WorkLocationsText);
        Assert.Equal("publication-1", viewModel.PublicationRef); Assert.Equal("Published: 2026-08-10T10:00:00Z", viewModel.GeneratedAtText);
    }

    [Fact]
    public async Task Empty_valid_snapshot_is_empty_not_unavailable()
    {
        var viewModel = CreateViewModel(Snapshot()); await viewModel.RefreshAsync();
        Assert.Equal(VocationOpportunityOverviewPresentationState.Empty, viewModel.State); Assert.True(viewModel.IsEmpty); Assert.False(viewModel.IsFailureState); Assert.Empty(viewModel.Opportunities); Assert.Contains("No opportunities", viewModel.StateText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Source_failures_map_to_distinct_bounded_presentation_states()
    {
        await AssertState(VocationOpportunityOverviewSourceFailureKind.Unavailable, VocationOpportunityOverviewPresentationState.Unavailable);
        await AssertState(VocationOpportunityOverviewSourceFailureKind.InvalidContract, VocationOpportunityOverviewPresentationState.InvalidContract);
        await AssertState(VocationOpportunityOverviewSourceFailureKind.IncompatibleContract, VocationOpportunityOverviewPresentationState.IncompatibleContract);
    }

    [Fact]
    public async Task Presentation_exposes_distinct_user_facing_state_information()
    {
        var viewModel = CreateViewModel(Snapshot());
        Assert.True(viewModel.IsLoading);
        Assert.False(viewModel.IsLoaded);

        await viewModel.RefreshAsync();
        Assert.True(viewModel.IsEmpty);
        Assert.False(viewModel.IsFailureState);
        Assert.Equal("No opportunities yet", viewModel.StateTitle);
        Assert.Equal("Vocation has not published any opportunities yet.", viewModel.StateDescription);
        Assert.Contains("Publication publication-1", viewModel.PublicationMetadataText, StringComparison.Ordinal);

        var states = new[]
        {
            VocationOpportunityOverviewSourceFailureKind.Unavailable,
            VocationOpportunityOverviewSourceFailureKind.InvalidContract,
            VocationOpportunityOverviewSourceFailureKind.IncompatibleContract
        };
        var failureTitles = new List<string>();
        foreach (var state in states)
        {
            var failed = CreateViewModel(new VocationOpportunityOverviewSourceException(state, "not shown"));
            await failed.RefreshAsync();
            Assert.True(failed.IsFailureState);
            Assert.False(failed.IsEmpty);
            Assert.False(string.IsNullOrWhiteSpace(failed.StateTitle));
            Assert.False(string.IsNullOrWhiteSpace(failed.StateDescription));
            failureTitles.Add(failed.StateTitle);
        }

        Assert.Equal(3, failureTitles.Distinct().Count());
    }

    [Fact]
    public async Task Refresh_replaces_items_and_supports_unavailable_to_loaded_recovery()
    {
        var source = new FakeSource(Snapshot(new VocationOpportunity("a", "First", new("c", "Company"), [], BigInteger.Zero)));
        var viewModel = new VocationOpportunityOverviewViewModel(new GetVocationOpportunityOverviewUseCase(source)); await viewModel.RefreshAsync();
        source.Next = new VocationOpportunityOverviewSourceException(VocationOpportunityOverviewSourceFailureKind.Unavailable, "not shown"); await viewModel.RefreshAsync();
        Assert.Equal(VocationOpportunityOverviewPresentationState.Unavailable, viewModel.State); Assert.Empty(viewModel.Opportunities);
        source.Next = Snapshot(new VocationOpportunity("b", "Second", new("c", "Company"), [], BigInteger.One)); await viewModel.RefreshAsync();
        Assert.Equal(VocationOpportunityOverviewPresentationState.Loaded, viewModel.State); Assert.Equal("Second", viewModel.Opportunities.Single().Title);
    }

    [Fact]
    public async Task Vocation_capability_opens_dedicated_presentation_and_disabled_capability_cannot_open()
    {
        var service = VocationIntegrationMetadata.ServiceId; var adapter = new VocationIntegrationAdapter(new FakeSource(Snapshot(new VocationOpportunity("a", "A", new("c", "Company"), [], BigInteger.One))));
        var integrations = new MemoryIntegrationStore(); var publications = new MemoryPublicationStore();
        var shell = new ShellViewModel(
            new EnsureCurrentDeviceUseCase(new MemoryDeviceStore()),
            new RegisterKnownIntegrationsUseCase(new StaticIntegrationAdapterCatalog([adapter]), integrations),
            new RefreshPublicationsUseCase(new StaticIntegrationAdapterCatalog([adapter]), publications),
            new ListServiceIntegrationsUseCase(integrations, publications),
            new SetGlobalIntegrationEnablementUseCase(integrations), new SetDeviceIntegrationOverrideUseCase(integrations), new ClearDeviceIntegrationOverrideUseCase(integrations),
            new ResolveCapabilityCatalogUseCase(new StaticIntegrationAdapterCatalog([adapter]), integrations, publications), "Windows PC", new GetVocationOpportunityOverviewUseCase(new FakeSource(Snapshot())));
        await shell.EnsureInitializedAsync(); Assert.False(shell.Capabilities.Single().CanOpen);
        await shell.EnableGloballyCommand.ExecuteAsync(null); Assert.True(shell.Capabilities.Single().CanOpen); await shell.OpenCapabilityCommand.ExecuteAsync(null);
        Assert.NotNull(shell.OpenedVocationOpportunityOverview); Assert.Equal(VocationOpportunityOverviewPresentationState.Empty, shell.OpenedVocationOpportunityOverview!.State);
    }

    [Fact]
    public async Task Jobs_is_a_first_class_desktop_product_destination_using_the_existing_read_path()
    {
        var adapter = new VocationIntegrationAdapter(new FakeSource(Snapshot(new VocationOpportunity("a", "A", new("c", "Company"), [], BigInteger.One))));
        var integrations = new MemoryIntegrationStore(); var publications = new MemoryPublicationStore();
        var source = new FakeSource(Snapshot(new VocationOpportunity("a", "A", new("c", "Company"), [], BigInteger.One)));
        var adapters = new StaticIntegrationAdapterCatalog([adapter]);
        var shell = new ShellViewModel(
            new EnsureCurrentDeviceUseCase(new MemoryDeviceStore()),
            new RegisterKnownIntegrationsUseCase(adapters, integrations),
            new RefreshPublicationsUseCase(adapters, publications),
            new ListServiceIntegrationsUseCase(integrations, publications),
            new SetGlobalIntegrationEnablementUseCase(integrations), new SetDeviceIntegrationOverrideUseCase(integrations), new ClearDeviceIntegrationOverrideUseCase(integrations),
            new ResolveCapabilityCatalogUseCase(adapters, integrations, publications), "Windows PC", new GetVocationOpportunityOverviewUseCase(source));

        await shell.EnsureInitializedAsync();
        Assert.False(shell.ShowJobsCommand.CanExecute(null));
        await shell.EnableGloballyCommand.ExecuteAsync(null);
        Assert.True(shell.ShowJobsCommand.CanExecute(null));
        await shell.ShowJobsCommand.ExecuteAsync(null);
        Assert.Equal(ShellSurface.Jobs, shell.CurrentSurface);
        Assert.True(shell.IsJobsVisible);
        Assert.Equal(VocationOpportunityOverviewPresentationState.Loaded, shell.OpenedVocationOpportunityOverview!.State);
        Assert.Equal("A", shell.OpenedVocationOpportunityOverview.Opportunities.Single().Title);
    }

    private static async Task AssertState(VocationOpportunityOverviewSourceFailureKind kind, VocationOpportunityOverviewPresentationState expected)
    {
        var viewModel = CreateViewModel(new VocationOpportunityOverviewSourceException(kind, "not shown")); await viewModel.RefreshAsync(); Assert.Equal(expected, viewModel.State);
    }

    private static VocationOpportunityOverviewViewModel CreateViewModel(object next) => new(new GetVocationOpportunityOverviewUseCase(new FakeSource(next)));

    private static VocationOpportunityOverview Snapshot(params VocationOpportunity[] opportunities) => new("publication-1", new("2026-08-10T10:00:00Z", new DateTimeOffset(2026, 8, 10, 10, 0, 0, TimeSpan.Zero)), opportunities);

    private sealed class FakeSource(object next) : IVocationOpportunityOverviewSource
    {
        public object Next { get; set; } = next;
        public ValueTask<VocationOpportunityOverview> GetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested(); if (Next is Exception exception) throw exception; return ValueTask.FromResult((VocationOpportunityOverview)Next);
        }
    }

    private sealed class MemoryDeviceStore : ILocalDeviceStore
    {
        private LocalDeviceConfiguration? value;
        public ValueTask<LocalDeviceConfiguration?> LoadAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(value);
        public ValueTask SaveAsync(LocalDeviceConfiguration configuration, CancellationToken cancellationToken = default) { value = configuration; return ValueTask.CompletedTask; }
    }
    private sealed class MemoryIntegrationStore : IServiceIntegrationStore
    {
        private readonly Dictionary<ServiceIdentity, ServiceIntegration> values = [];
        public ValueTask<ServiceIntegration?> LoadAsync(ServiceIdentity id, CancellationToken cancellationToken = default) => ValueTask.FromResult(values.GetValueOrDefault(id));
        public ValueTask<IReadOnlyList<ServiceIntegration>> LoadAllAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult<IReadOnlyList<ServiceIntegration>>(values.Values.ToArray());
        public ValueTask SaveAsync(ServiceIntegration integration, CancellationToken cancellationToken = default) { values[integration.ServiceIdentity] = integration; return ValueTask.CompletedTask; }
    }
    private sealed class MemoryPublicationStore : IIntegrationPublicationStore
    {
        private readonly Dictionary<ServiceIdentity, IntegrationPublicationState> values = [];
        public ValueTask SaveAsync(IntegrationPublicationState state, CancellationToken cancellationToken = default) { values[state.ServiceIdentity] = state; return ValueTask.CompletedTask; }
        public ValueTask<IntegrationPublicationState> LoadAsync(ServiceIdentity id, CancellationToken cancellationToken = default) => ValueTask.FromResult(values.GetValueOrDefault(id) ?? new IntegrationPublicationState(id, null, PublicationRefreshObservation.NotAttempted));
    }
}
