using WiiiiGotThis.Application;
using WiiiiGotThis.Presentation;

namespace WiiiiGotThis.Integration.Tests;

public sealed class VocationMapWorkspaceTests
{
    [Fact]
    public async Task Loaded_map_exposes_compact_workspace_state_and_selection_without_redefining_provider_data()
    {
        var snapshot = new VocationMapProjection(
            "map-publication",
            new("2026-08-17T06:00:00Z", new DateTimeOffset(2026, 8, 17, 6, 0, 0, TimeSpan.Zero)),
            [
                new("feature-one", "opportunity-one", "Platform Engineer", new("company-one", "Company One"), new("Hamburg", "city"), new(53.55, 10.0)),
                new("feature-two", "opportunity-two", "Backend Developer", new("company-two", "Company Two"), new("Berlin", "exact_address"), new(52.52, 13.405))
            ]);
        var viewModel = new VocationMapProjectionViewModel(
            new GetVocationMapProjectionUseCase(new SnapshotSource(snapshot)));

        await viewModel.RefreshAsync();

        Assert.True(viewModel.IsLoaded);
        Assert.False(viewModel.IsStateBannerVisible);
        Assert.Equal("2 locations", viewModel.FeatureCountText);
        Assert.True(viewModel.IsFeatureSelectionEmpty);
        Assert.False(viewModel.IsFeatureSelected);

        var selected = viewModel.Features[1];
        viewModel.SelectFeature(selected);

        Assert.True(viewModel.IsFeatureSelected);
        Assert.False(viewModel.IsFeatureSelectionEmpty);
        Assert.Equal("Backend Developer", viewModel.SelectedFeature!.Title);
        Assert.Equal("Company Two", viewModel.SelectedFeature.CompanyName);
        Assert.Equal("Berlin", viewModel.SelectedFeature.WorkLocationLabel);
        Assert.Equal("exact_address", viewModel.SelectedFeature.WorkLocationPrecision);
        Assert.Equal(52.52, viewModel.SelectedFeature.Latitude);
        Assert.Equal(13.405, viewModel.SelectedFeature.Longitude);
    }

    [Fact]
    public async Task Refresh_clears_selection_and_failure_states_replace_the_map_workspace()
    {
        var source = new MutableSource(new VocationMapProjection(
            "map-publication",
            new("2026-08-17T06:00:00Z", new DateTimeOffset(2026, 8, 17, 6, 0, 0, TimeSpan.Zero)),
            [new("feature", "opportunity", "Role", new("company", "Company"), new("Hamburg", "city"), new(53.55, 10.0))]));
        var viewModel = new VocationMapProjectionViewModel(new GetVocationMapProjectionUseCase(source));

        await viewModel.RefreshAsync();
        viewModel.SelectFeature(viewModel.Features.Single());
        Assert.True(viewModel.IsFeatureSelected);

        source.Next = new VocationMapProjectionSourceException(
            VocationMapProjectionSourceFailureKind.Unavailable,
            "offline");
        await viewModel.RefreshAsync();

        Assert.True(viewModel.IsUnavailable);
        Assert.True(viewModel.IsStateBannerVisible);
        Assert.False(viewModel.IsFeatureSelected);
        Assert.False(viewModel.IsFeatureSelectionEmpty);
        Assert.Empty(viewModel.Features);
    }

    private sealed class SnapshotSource(VocationMapProjection snapshot) : IVocationMapProjectionSource
    {
        public ValueTask<VocationMapProjection> GetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(snapshot);
        }
    }

    private sealed class MutableSource(object next) : IVocationMapProjectionSource
    {
        public object Next { get; set; } = next;

        public ValueTask<VocationMapProjection> GetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Next is Exception exception)
                throw exception;
            return ValueTask.FromResult((VocationMapProjection)Next);
        }
    }
}
