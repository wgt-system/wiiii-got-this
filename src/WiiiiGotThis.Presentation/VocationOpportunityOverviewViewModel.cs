using System.Collections.ObjectModel;
using System.Globalization;
using System.Numerics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

public enum VocationOpportunityOverviewPresentationState
{
    Loading,
    Loaded,
    Empty,
    Unavailable,
    InvalidContract,
    IncompatibleContract
}

public sealed class VocationOpportunityItemViewModel(VocationOpportunity opportunity)
{
    public string Title => opportunity.Title;
    public string CompanyName => opportunity.Company.Name;
    public IReadOnlyList<string> WorkLocations => opportunity.WorkLocations.Select(location => location.Label).ToArray();
    public string WorkLocationsText => WorkLocations.Count == 0 ? "No location published" : string.Join(" · ", WorkLocations);
    public BigInteger PostingCount => opportunity.PostingCount;
    public string PostingCountText => $"{opportunity.PostingCount.ToString(CultureInfo.InvariantCulture)} {(opportunity.PostingCount == BigInteger.One ? "posting" : "postings")}";
}

public sealed partial class VocationOpportunityOverviewViewModel : ObservableObject
{
    private readonly GetVocationOpportunityOverviewUseCase readOverview;

    [ObservableProperty] private VocationOpportunityOverviewPresentationState state = VocationOpportunityOverviewPresentationState.Loading;
    [ObservableProperty] private string? publicationRef;
    [ObservableProperty] private string? generatedAtRawValue;
    [ObservableProperty] private DateTimeOffset? generatedAtNormalizedUtc;

    public VocationOpportunityOverviewViewModel(GetVocationOpportunityOverviewUseCase readOverview)
    {
        this.readOverview = readOverview;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    public ObservableCollection<VocationOpportunityItemViewModel> Opportunities { get; } = [];
    public IAsyncRelayCommand RefreshCommand { get; }
    public bool IsLoading => State == VocationOpportunityOverviewPresentationState.Loading;
    public string StateText => State switch
    {
        VocationOpportunityOverviewPresentationState.Loading => "Loading opportunities…",
        VocationOpportunityOverviewPresentationState.Loaded => "Current opportunities published by Vocation",
        VocationOpportunityOverviewPresentationState.Empty => "No opportunities are currently published by Vocation.",
        VocationOpportunityOverviewPresentationState.Unavailable => "Vocation is currently unavailable.",
        VocationOpportunityOverviewPresentationState.InvalidContract => "Vocation returned invalid provider data.",
        VocationOpportunityOverviewPresentationState.IncompatibleContract => "This Vocation capability version is not supported.",
        _ => "The Vocation opportunity overview could not be displayed."
    };
    public string GeneratedAtText => GeneratedAtRawValue is { Length: > 0 } raw ? $"Published: {raw}" : string.Empty;

    public async Task RefreshAsync()
    {
        State = VocationOpportunityOverviewPresentationState.Loading;
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(StateText));
        Opportunities.Clear();
        PublicationRef = null;
        GeneratedAtRawValue = null;
        GeneratedAtNormalizedUtc = null;

        var result = await readOverview.ExecuteAsync();
        if (result.Snapshot is { } snapshot && result.Status == VocationOpportunityOverviewReadStatus.Loaded)
        {
            PublicationRef = snapshot.PublicationRef;
            GeneratedAtRawValue = snapshot.GeneratedAt.RawValue;
            GeneratedAtNormalizedUtc = snapshot.GeneratedAt.NormalizedUtc;
            foreach (var opportunity in snapshot.Opportunities)
                Opportunities.Add(new VocationOpportunityItemViewModel(opportunity));
            State = Opportunities.Count == 0
                ? VocationOpportunityOverviewPresentationState.Empty
                : VocationOpportunityOverviewPresentationState.Loaded;
        }
        else
        {
            State = result.Status switch
            {
                VocationOpportunityOverviewReadStatus.Unavailable => VocationOpportunityOverviewPresentationState.Unavailable,
                VocationOpportunityOverviewReadStatus.InvalidContract => VocationOpportunityOverviewPresentationState.InvalidContract,
                VocationOpportunityOverviewReadStatus.IncompatibleContract => VocationOpportunityOverviewPresentationState.IncompatibleContract,
                _ => VocationOpportunityOverviewPresentationState.Unavailable
            };
        }

        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(StateText));
        OnPropertyChanged(nameof(GeneratedAtText));
    }
}
