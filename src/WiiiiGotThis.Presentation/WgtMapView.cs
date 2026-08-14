using Avalonia.Controls;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Avalonia;

namespace WiiiiGotThis.Presentation;

public sealed class WgtMapView : Grid, IDisposable
{
    private readonly MapControl mapControl = new();
    private readonly Map map = new();
    private readonly MemoryLayer featureLayer = new("Vocation opportunities");
    private readonly Dictionary<string, (VocationMapFeaturePresentationViewModel Feature, PointFeature MapFeature)> mapFeatures = [];
    private VocationMapProjectionViewModel? viewModel;
    private bool disposed;

    public WgtMapView()
    {
        Children.Add(mapControl);
        mapControl.Map = map;
        map.Layers.Add(OpenStreetMap.CreateTileLayer());
        map.Layers.Add(featureLayer);
        mapControl.Info += OnMapInfo;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (viewModel is not null)
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        viewModel = DataContext as VocationMapProjectionViewModel;
        if (viewModel is not null)
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        RebuildFeatures();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(VocationMapProjectionViewModel.Features)
            or nameof(VocationMapProjectionViewModel.State)
            or nameof(VocationMapProjectionViewModel.SelectedFeature))
            RebuildFeatures();
    }

    private void RebuildFeatures()
    {
        var hadFeatures = mapFeatures.Count > 0;
        mapFeatures.Clear();
        var features = new List<IFeature>();
        if (viewModel?.IsLoaded == true)
        {
            foreach (var feature in viewModel.Features)
            {
                var mapFeature = new PointFeature(SphericalMercator.FromLonLat(feature.Longitude, feature.Latitude));
                mapFeature.Data = feature;
                mapFeature.Styles = [CreatePointStyle(ReferenceEquals(feature, viewModel.SelectedFeature))];
                mapFeatures[feature.FeatureRef] = (feature, mapFeature);
                features.Add(mapFeature);
            }
        }

        featureLayer.Features = features;
        mapControl.RefreshData(ChangeType.Discrete);
        if (features.Count > 0 && !hadFeatures)
            map.Navigator.ZoomToPanBounds();
    }

    private static SymbolStyle CreatePointStyle(bool selected) => new()
    {
        SymbolScale = selected ? 1.2 : 0.9,
        Fill = new Brush(Color.DodgerBlue),
        Outline = new Pen(Color.White, selected ? 3 : 2)
    };

    private void OnMapInfo(object? sender, MapInfoEventArgs e)
    {
        var info = e.GetMapInfo?.Invoke(e.Map.Layers);
        if (info?.Feature?.Data is VocationMapFeaturePresentationViewModel feature)
            viewModel?.SelectFeature(feature);
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        mapControl.Info -= OnMapInfo;
        mapControl.Unsubscribe();
        if (viewModel is not null)
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }
}
