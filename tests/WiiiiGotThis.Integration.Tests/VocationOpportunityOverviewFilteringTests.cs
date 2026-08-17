using System.Numerics;
using WiiiiGotThis.Application;
using WiiiiGotThis.Presentation;

namespace WiiiiGotThis.Integration.Tests;

public sealed class VocationOpportunityOverviewFilteringTests
{
    [Fact]
    public async Task Local_filtering_and_sorting_use_only_published_fields_and_can_restore_provider_order()
    {
        var snapshot = new VocationOpportunityOverview(
            "publication-filtering",
            new("2026-08-17T06:00:00Z", new DateTimeOffset(2026, 8, 17, 6, 0, 0, TimeSpan.Zero)),
            [
                new("one", "Platform Engineer", new("company-z", "Zulu GmbH"), [new("Berlin", "Berlin", null, "DE", "city")], new BigInteger(2)),
                new("two", "Backend Developer", new("company-a", "Alpha GmbH"), [new("Hamburg", "Hamburg", null, "DE", "city")], new BigInteger(5)),
                new("three", "Researcher", new("company-b", "Beta GmbH"), [new("Remote", null, null, null, "unknown")], BigInteger.One)
            ]);
        var viewModel = new VocationOpportunityOverviewViewModel(
            new GetVocationOpportunityOverviewUseCase(new SnapshotSource(snapshot)));

        await viewModel.RefreshAsync();

        Assert.Equal(["Platform Engineer", "Backend Developer", "Researcher"], viewModel.VisibleOpportunities.Select(item => item.Title));
        Assert.Equal("3 opportunities", viewModel.ResultCountText);

        viewModel.SearchText = "berlin";
        Assert.Equal(["Platform Engineer"], viewModel.VisibleOpportunities.Select(item => item.Title));
        Assert.Equal("1 of 3 opportunities", viewModel.ResultCountText);

        viewModel.SearchText = "alpha";
        Assert.Equal(["Backend Developer"], viewModel.VisibleOpportunities.Select(item => item.Title));

        viewModel.SearchText = string.Empty;
        viewModel.SelectedSortOption = VocationOpportunityOverviewViewModel.TitleSort;
        Assert.Equal(["Backend Developer", "Platform Engineer", "Researcher"], viewModel.VisibleOpportunities.Select(item => item.Title));

        viewModel.SelectedSortOption = VocationOpportunityOverviewViewModel.CompanySort;
        Assert.Equal(["Backend Developer", "Researcher", "Platform Engineer"], viewModel.VisibleOpportunities.Select(item => item.Title));

        viewModel.SelectedSortOption = VocationOpportunityOverviewViewModel.PostingCountSort;
        Assert.Equal(["Backend Developer", "Platform Engineer", "Researcher"], viewModel.VisibleOpportunities.Select(item => item.Title));

        viewModel.SelectedSortOption = VocationOpportunityOverviewViewModel.ProviderOrderSort;
        Assert.Equal(["Platform Engineer", "Backend Developer", "Researcher"], viewModel.VisibleOpportunities.Select(item => item.Title));

        viewModel.SearchText = "does-not-exist";
        Assert.Empty(viewModel.VisibleOpportunities);
        Assert.True(viewModel.HasNoMatches);
        Assert.False(viewModel.HasVisibleOpportunities);
    }

    private sealed class SnapshotSource(VocationOpportunityOverview snapshot) : IVocationOpportunityOverviewSource
    {
        public ValueTask<VocationOpportunityOverview> GetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(snapshot);
        }
    }
}
