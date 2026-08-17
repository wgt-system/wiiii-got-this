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

public sealed class VocationOpportunityItemViewModel
{
    public VocationOpportunityItemViewModel(VocationOpportunity opportunity)
    {
        Title = opportunity.Title;
        CompanyName = opportunity.Company.Name;
        WorkLocations = opportunity.WorkLocations.Select(location => location.Label).ToArray();
        WorkLocationsText = WorkLocations.Count == 0 ? "No location published" : string.Join(" · ", WorkLocations);
        PostingCount = opportunity.PostingCount;
        PostingCountText = $"{PostingCount.ToString(CultureInfo.InvariantCulture)} {(PostingCount == BigInteger.One ? "posting" : "postings")}";
    }

    public string Title { get; }
    public string CompanyName { get; }
    public IReadOnlyList<string> WorkLocations { get; }
    public string WorkLocationsText { get; }
    public BigInteger PostingCount { get; }
    public string PostingCountText { get; }

    public bool Matches(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return true;

        var term = query.Trim();
        return Title.Contains(term, StringComparison.OrdinalIgnoreCase)
            || CompanyName.Contains(term, StringComparison.OrdinalIgnoreCase)
            || WorkLocations.Any(location => location.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed partial class VocationOpportunityOverviewViewModel : ObservableObject
{
    public const string ProviderOrderSort = "Provider order";
    public const string TitleSort = "Title";
    public const string CompanySort = "Company";
    public const string PostingCountSort = "Most postings";

    private readonly GetVocationOpportunityOverviewUseCase readOverview;

    [ObservableProperty] private VocationOpportunityOverviewPresentationState state = VocationOpportunityOverviewPresentationState.Loading;
    [ObservableProperty] private string? publicationRef;
    [ObservableProperty] private string? generatedAtRawValue;
    [ObservableProperty] private DateTimeOffset? generatedAtNormalizedUtc;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedSortOption = ProviderOrderSort;

    public VocationOpportunityOverviewViewModel(GetVocationOpportunityOverviewUseCase readOverview)
    {
        this.readOverview = readOverview;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    public ObservableCollection<VocationOpportunityItemViewModel> Opportunities { get; } = [];
    public ObservableCollection<VocationOpportunityItemViewModel> VisibleOpportunities { get; } = [];
    public IReadOnlyList<string> SortOptions { get; } = [ProviderOrderSort, TitleSort, CompanySort, PostingCountSort];
    public IAsyncRelayCommand RefreshCommand { get; }
    public bool IsLoading => State == VocationOpportunityOverviewPresentationState.Loading;
    public bool IsLoaded => State == VocationOpportunityOverviewPresentationState.Loaded;
    public bool IsEmpty => State == VocationOpportunityOverviewPresentationState.Empty;
    public bool IsUnavailable => State == VocationOpportunityOverviewPresentationState.Unavailable;
    public bool IsInvalidContract => State == VocationOpportunityOverviewPresentationState.InvalidContract;
    public bool IsIncompatibleContract => State == VocationOpportunityOverviewPresentationState.IncompatibleContract;
    public bool IsFailureState => IsUnavailable || IsInvalidContract || IsIncompatibleContract;
    public bool IsStateBannerVisible => !IsLoaded;
    public bool HasVisibleOpportunities => IsLoaded && VisibleOpportunities.Count > 0;
    public bool HasNoMatches => IsLoaded && Opportunities.Count > 0 && VisibleOpportunities.Count == 0;
    public bool HasPublicationMetadata => !string.IsNullOrWhiteSpace(PublicationMetadataText);
    public string ResultCountText => IsLoaded
        ? SearchText.Length == 0 && VisibleOpportunities.Count == Opportunities.Count
            ? $"{Opportunities.Count.ToString(CultureInfo.InvariantCulture)} opportunities"
            : $"{VisibleOpportunities.Count.ToString(CultureInfo.InvariantCulture)} of {Opportunities.Count.ToString(CultureInfo.InvariantCulture)} opportunities"
        : string.Empty;
    public string StateTitle => State switch
    {
        VocationOpportunityOverviewPresentationState.Loading => "Loading opportunities",
        VocationOpportunityOverviewPresentationState.Loaded => "Current opportunities",
        VocationOpportunityOverviewPresentationState.Empty => "No opportunities yet",
        VocationOpportunityOverviewPresentationState.Unavailable => "Vocation is unavailable",
        VocationOpportunityOverviewPresentationState.InvalidContract => "Opportunity data could not be read",
        VocationOpportunityOverviewPresentationState.IncompatibleContract => "This capability version is not supported",
        _ => "Opportunity overview"
    };
    public string StateDescription => State switch
    {
        VocationOpportunityOverviewPresentationState.Loading => "Fetching the latest published overview.",
        VocationOpportunityOverviewPresentationState.Loaded => "Published opportunities from Vocation.",
        VocationOpportunityOverviewPresentationState.Empty => "Vocation has not published any opportunities yet.",
        VocationOpportunityOverviewPresentationState.Unavailable => "Vocation could not be reached. Try refreshing again later.",
        VocationOpportunityOverviewPresentationState.InvalidContract => "Vocation returned data that did not match the accepted contract.",
        VocationOpportunityOverviewPresentationState.IncompatibleContract => "This Vocation capability version is not supported by WGT.",
        _ => "The opportunity overview could not be displayed."
    };
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
    public string PublicationMetadataText => string.Join(" · ", new[]
    {
        PublicationRef is { Length: > 0 } reference ? $"Publication {reference}" : null,
        GeneratedAtText is { Length: > 0 } generated ? generated : null
    }.Where(value => value is not null));

    partial void OnStateChanged(VocationOpportunityOverviewPresentationState value)
    {
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(IsLoaded));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsUnavailable));
        OnPropertyChanged(nameof(IsInvalidContract));
        OnPropertyChanged(nameof(IsIncompatibleContract));
        OnPropertyChanged(nameof(IsFailureState));
        OnPropertyChanged(nameof(IsStateBannerVisible));
        OnPropertyChanged(nameof(HasVisibleOpportunities));
        OnPropertyChanged(nameof(HasNoMatches));
        OnPropertyChanged(nameof(ResultCountText));
        OnPropertyChanged(nameof(StateTitle));
        OnPropertyChanged(nameof(StateDescription));
        OnPropertyChanged(nameof(StateText));
    }

    partial void OnPublicationRefChanged(string? value)
    {
        OnPropertyChanged(nameof(PublicationMetadataText));
        OnPropertyChanged(nameof(HasPublicationMetadata));
    }

    partial void OnGeneratedAtRawValueChanged(string? value)
    {
        OnPropertyChanged(nameof(GeneratedAtText));
        OnPropertyChanged(nameof(PublicationMetadataText));
        OnPropertyChanged(nameof(HasPublicationMetadata));
    }

    partial void OnSearchTextChanged(string value) => RebuildVisibleOpportunities();

    partial void OnSelectedSortOptionChanged(string value) => RebuildVisibleOpportunities();

    public async Task RefreshAsync()
    {
        State = VocationOpportunityOverviewPresentationState.Loading;
        Opportunities.Clear();
        VisibleOpportunities.Clear();
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
            RebuildVisibleOpportunities();
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

        OnPropertyChanged(nameof(PublicationMetadataText));
    }

    private void RebuildVisibleOpportunities()
    {
        IEnumerable<VocationOpportunityItemViewModel> visible = Opportunities.Where(item => item.Matches(SearchText));
        visible = SelectedSortOption switch
        {
            TitleSort => visible.OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase),
            CompanySort => visible.OrderBy(item => item.CompanyName, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase),
            PostingCountSort => visible.OrderByDescending(item => item.PostingCount),
            _ => visible
        };

        VisibleOpportunities.Clear();
        foreach (var opportunity in visible)
            VisibleOpportunities.Add(opportunity);

        OnPropertyChanged(nameof(HasVisibleOpportunities));
        OnPropertyChanged(nameof(HasNoMatches));
        OnPropertyChanged(nameof(ResultCountText));
    }
}
