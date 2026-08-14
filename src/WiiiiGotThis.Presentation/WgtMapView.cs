using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Themes.Fluent;

namespace WiiiiGotThis.Presentation;

public sealed class WgtMapView : Control
{
    private VocationMapProjectionViewModel? viewModel;
    private Point? panStart;
    private Vector panOffset;
    private double zoom = 1d;

    public WgtMapView()
    {
        DataContextChanged += OnDataContextChanged;
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerWheelChanged += OnPointerWheelChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (viewModel is not null)
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        viewModel = DataContext as VocationMapProjectionViewModel;
        if (viewModel is not null)
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        InvalidateVisual();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => InvalidateVisual();

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = new Rect(Bounds.Size);
        context.DrawRectangle(GetBrush("WgtMapBackgroundBrush", Brushes.Transparent), null, bounds);
        if (viewModel is null || !viewModel.IsLoaded)
            return;

        var gridPen = new Pen(GetBrush("WgtMapGridBrush", Brushes.Gray), 1);
        var width = bounds.Width;
        var height = bounds.Height;
        for (var longitude = -180; longitude <= 180; longitude += 30)
        {
            var x = ProjectX(longitude, width);
            context.DrawLine(gridPen, new Point(x, 0), new Point(x, height));
        }
        for (var latitude = -60; latitude <= 60; latitude += 15)
        {
            var y = ProjectY(latitude, height);
            context.DrawLine(gridPen, new Point(0, y), new Point(width, y));
        }

        var pointBrush = GetBrush("WgtMapPointBrush", Brushes.DodgerBlue);
        foreach (var feature in viewModel.Features)
        {
            var point = new Point(ProjectX(feature.Longitude, width), ProjectY(feature.Latitude, height));
            var radius = ReferenceEquals(feature, viewModel.SelectedFeature) ? 9 : 6;
            context.DrawEllipse(pointBrush, null, point, radius, radius);
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        panStart = e.GetPosition(this);
        e.Pointer.Capture(this);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (panStart is not { } start || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        var current = e.GetPosition(this);
        panOffset += current - start;
        panStart = current;
        InvalidateVisual();
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (panStart is not { } start)
            return;
        var current = e.GetPosition(this);
        e.Pointer.Capture(null);
        panStart = null;
        if (Distance(current, start) <= 6)
            SelectNearest(current);
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        zoom = Math.Clamp(zoom * (e.Delta.Y > 0 ? 1.15 : 1 / 1.15), 1, 8);
        InvalidateVisual();
        e.Handled = true;
    }

    private void SelectNearest(Point position)
    {
        if (viewModel is null)
            return;
        VocationMapFeaturePresentationViewModel? nearest = null;
        var nearestDistance = 18d;
        foreach (var feature in viewModel.Features)
        {
            var point = new Point(ProjectX(feature.Longitude, Bounds.Width), ProjectY(feature.Latitude, Bounds.Height));
            var distance = Distance(point, position);
            if (distance < nearestDistance)
            {
                nearest = feature;
                nearestDistance = distance;
            }
        }
        viewModel.SelectFeature(nearest);
    }

    private static double Distance(Point first, Point second) => Math.Sqrt(Math.Pow(first.X - second.X, 2) + Math.Pow(first.Y - second.Y, 2));

    private double ProjectX(double longitude, double width) => (longitude + 180) / 360 * width * zoom + panOffset.X + width * (1 - zoom) / 2;

    private double ProjectY(double latitude, double height)
    {
        var clamped = Math.Clamp(latitude, -85.05, 85.05) * Math.PI / 180;
        var mercator = Math.Log(Math.Tan(Math.PI / 4 + clamped / 2));
        var normalized = 0.5 - mercator / (2 * Math.PI);
        return normalized * height * zoom + panOffset.Y + height * (1 - zoom) / 2;
    }

    private IBrush GetBrush(string key, IBrush fallback)
    {
        if (Avalonia.Application.Current is { } app && app.TryGetResource(key, ActualThemeVariant, out var resource) && resource is IBrush brush)
            return brush;
        return fallback;
    }
}
