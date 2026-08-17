using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

public enum VocationMapProjectionPresentationState
{
    Loading,
    Loaded,
    Empty,
    Unavailable,
    InvalidContract,
    IncompatibleContract
}

public sealed class VocationMapFeaturePresentationViewModel(VocationMapFeature feature)
{
    public string FeatureRef => feature.FeatureRef;
    public string Title => feature.Title;
    public string CompanyName => feature.Company.Name;
    public string WorkLocationLabel => feature.WorkLocation.Label;
    public string WorkLocationPrecision => feature.WorkLocation.Precision;
    public double Latitude => feature.Coordinates.Latitude;
    public double Longitude => feature.Coordinates.Longitude;
}

public sealed partial class VocationMapProjectionViewModel : ObservableObject
{
    private readonly GetVocationMapProjectionUseCase readProjection;

    [ObservableProperty] private VocationMapProjectionPresentationState state = VocationMapProjectionPresentationState.Loading;
    [ObservableProperty] private string? publicationRef;
    [ObservableProperty] private string? generatedAtRawValue;
    [ObservableProperty] private VocationMapFeaturePresentationViewModel? selectedFeature;

    public VocationMapProjectionViewModel(GetVocationMapProjectionUseCase readProjection)
    {
        this.readProjection = readProjection;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    public ObservableCollection<VocationMapFeaturePresentationViewModel> Features { get; } = [];
    public IAsyncRelayCommand RefreshCommand { get; }
    public bool IsLoading => State == VocationMapProjectionPresentationState.Loading;
    public bool IsLoaded => State == VocationMapProjectionPresentationState.Loaded;
    public bool IsEmpty => State == VocationMapProjectionPresentationState.Empty;
    public bool IsUnavailable => State == VocationMapProjectionPresentationState.Unavailable;
    public bool IsInvalidContract => State == VocationMapProjectionPresentationState.InvalidContract;
    public bool IsIncompatibleContract => State == VocationMapProjectionPresentationState.IncompatibleContract;
    public bool IsStateBannerVisible => !IsLoaded;
    public string FeatureCountText => IsLoaded
        ? $"{Features.Count.ToString(CultureInfo.InvariantCulture)} {(Features.Count == 1 ? "location" : "locations")}"
        : string.Empty;
    public string StateTitle => State switch
    {
        VocationMapProjectionPresentationState.Loading => "Loading map",
        VocationMapProjectionPresentationState.Loaded => "Opportunity map",
        VocationMapProjectionPresentationState.Empty => "No opportunities to map",
        VocationMapProjectionPresentationState.Unavailable => "Vocation map is unavailable",
        VocationMapProjectionPresentationState.InvalidContract => "Map data could not be read",
        VocationMapProjectionPresentationState.IncompatibleContract => "This map version is not supported",
        _ => "Opportunity map"
    };
    public string StateDescription => State switch
    {
        VocationMapProjectionPresentationState.Loading => "Fetching published opportunity locations.",
        VocationMapProjectionPresentationState.Loaded => "Published opportunity locations from Vocation.",
        VocationMapProjectionPresentationState.Empty => "Vocation has not published any mappable opportunities yet.",
        VocationMapProjectionPresentationState.Unavailable => "Vocation could not be reached. Try refreshing again later.",
        VocationMapProjectionPresentationState.InvalidContract => "Vocation returned map data that did not match the accepted contract.",
        VocationMapProjectionPresentationState.IncompatibleContract => "This Vocation map capability version is not supported by WGT.",
        _ => "The opportunity map could not be displayed."
    };
    public string PublicationMetadataText => string.Join(" · ", new[]
    {
        PublicationRef is { Length: > 0 } reference ? $"Publication {reference}" : null,
        GeneratedAtRawValue is { Length: > 0 } generated ? $"Published: {generated}" : null
    }.Where(value => value is not null));
    public bool HasPublicationMetadata => !string.IsNullOrWhiteSpace(PublicationMetadataText);
    public bool IsFeatureSelected => SelectedFeature is not null;
    public bool IsFeatureSelectionEmpty => IsLoaded && SelectedFeature is null;

    partial void OnStateChanged(VocationMapProjectionPresentationState value)
    {
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(IsLoaded));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsUnavailable));
        OnPropertyChanged(nameof(IsInvalidContract));
        OnPropertyChanged(nameof(IsIncompatibleContract));
        OnPropertyChanged(nameof(IsStateBannerVisible));
        OnPropertyChanged(nameof(FeatureCountText));
        OnPropertyChanged(nameof(IsFeatureSelectionEmpty));
        OnPropertyChanged(nameof(StateTitle));
        OnPropertyChanged(nameof(StateDescription));
    }

    partial void OnPublicationRefChanged(string? value)
    {
        OnPropertyChanged(nameof(PublicationMetadataText));
        OnPropertyChanged(nameof(HasPublicationMetadata));
    }

    partial void OnGeneratedAtRawValueChanged(string? value)
    {
        OnPropertyChanged(nameof(PublicationMetadataText));
        OnPropertyChanged(nameof(HasPublicationMetadata));
    }

    partial void OnSelectedFeatureChanged(VocationMapFeaturePresentationViewModel? value)
    {
        OnPropertyChanged(nameof(IsFeatureSelected));
        OnPropertyChanged(nameof(IsFeatureSelectionEmpty));
    }

    public void SelectFeature(VocationMapFeaturePresentationViewModel? feature) => SelectedFeature = feature;

    public async Task RefreshAsync()
    {
        State = VocationMapProjectionPresentationState.Loading;
        Features.Clear();
        SelectedFeature = null;
        PublicationRef = null;
        GeneratedAtRawValue = null;

        var result = await readProjection.ExecuteAsync();
        if (result.Snapshot is { } snapshot && result.Status == VocationMapProjectionReadStatus.Loaded)
        {
            PublicationRef = snapshot.PublicationRef;
            GeneratedAtRawValue = snapshot.GeneratedAt.RawValue;
            foreach (var feature in snapshot.Features)
                Features.Add(new VocationMapFeaturePresentationViewModel(feature));
            State = Features.Count == 0
                ? VocationMapProjectionPresentationState.Empty
                : VocationMapProjectionPresentationState.Loaded;
            OnPropertyChanged(nameof(FeatureCountText));
        }
        else
        {
            State = result.Status switch
            {
                VocationMapProjectionReadStatus.Unavailable => VocationMapProjectionPresentationState.Unavailable,
                VocationMapProjectionReadStatus.InvalidContract => VocationMapProjectionPresentationState.InvalidContract,
                VocationMapProjectionReadStatus.IncompatibleContract => VocationMapProjectionPresentationState.IncompatibleContract,
                _ => VocationMapProjectionPresentationState.Unavailable
            };
        }

        OnPropertyChanged(nameof(PublicationMetadataText));
        OnPropertyChanged(nameof(HasPublicationMetadata));
    }
}
