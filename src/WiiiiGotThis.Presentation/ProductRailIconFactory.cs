using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using RailPath = Avalonia.Controls.Shapes.Path;

namespace WiiiiGotThis.Presentation;

/// <summary>
/// Small WGT-owned vector marks for hosted-product chrome. Keeping these independent of
/// platform fonts prevents the narrow rail from degrading into inconsistent Unicode glyphs
/// across Windows, macOS, Linux and future mobile hosts.
/// </summary>
internal static class ProductRailIconFactory
{
    public static Control CreateAtlas(double size = 18) => Create("atlas", size, BuildAtlasGeometry());

    public static Control CreateSettings(double size = 18) => Create("settings", size, BuildSettingsGeometry());

    public static Control CreateCapability(string? capabilityId, double size = 18) => capabilityId switch
    {
        BuildAtlasProjectionUseCase.OrientationGeospatialCapabilityId =>
            Create("geospatial", size, BuildGeospatialGeometry()),
        BuildAtlasProjectionUseCase.ConveyanceDurableDeliveryCapabilityId =>
            Create("delivery", size, BuildDeliveryGeometry()),
        _ => Create("capability", size, BuildCapabilityGeometry())
    };

    private static Control Create(string kind, double size, StreamGeometry geometry)
    {
        var path = new RailPath
        {
            Width = size,
            Height = size,
            Stretch = Stretch.Uniform,
            StrokeThickness = 1.8,
            StrokeLineCap = PenLineCap.Round,
            StrokeJoin = PenLineJoin.Round,
            Data = geometry,
            IsHitTestVisible = false,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        path.Classes.Add("wgt-product-rail-icon");
        path.Classes.Add(kind);

        return new Grid
        {
            Width = size,
            Height = size,
            IsHitTestVisible = false,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Children = { path }
        };
    }

    private static StreamGeometry BuildAtlasGeometry()
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(new Point(12, 3), isFilled: false);
        context.LineTo(new Point(12, 8));
        context.EndFigure(isClosed: false);
        context.BeginFigure(new Point(12, 16), isFilled: false);
        context.LineTo(new Point(12, 21));
        context.EndFigure(isClosed: false);
        context.BeginFigure(new Point(3, 12), isFilled: false);
        context.LineTo(new Point(8, 12));
        context.EndFigure(isClosed: false);
        context.BeginFigure(new Point(16, 12), isFilled: false);
        context.LineTo(new Point(21, 12));
        context.EndFigure(isClosed: false);
        context.BeginFigure(new Point(12, 8), isFilled: false);
        context.LineTo(new Point(16, 12));
        context.LineTo(new Point(12, 16));
        context.LineTo(new Point(8, 12));
        context.LineTo(new Point(12, 8));
        context.EndFigure(isClosed: false);
        return geometry;
    }

    private static StreamGeometry BuildSettingsGeometry()
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(new Point(4, 7), isFilled: false);
        context.LineTo(new Point(20, 7));
        context.EndFigure(isClosed: false);
        context.BeginFigure(new Point(4, 12), isFilled: false);
        context.LineTo(new Point(20, 12));
        context.EndFigure(isClosed: false);
        context.BeginFigure(new Point(4, 17), isFilled: false);
        context.LineTo(new Point(20, 17));
        context.EndFigure(isClosed: false);
        context.BeginFigure(new Point(8, 5), isFilled: false);
        context.LineTo(new Point(8, 9));
        context.EndFigure(isClosed: false);
        context.BeginFigure(new Point(16, 10), isFilled: false);
        context.LineTo(new Point(16, 14));
        context.EndFigure(isClosed: false);
        context.BeginFigure(new Point(11, 15), isFilled: false);
        context.LineTo(new Point(11, 19));
        context.EndFigure(isClosed: false);
        return geometry;
    }

    private static StreamGeometry BuildGeospatialGeometry()
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(new Point(12, 3), isFilled: false);
        context.LineTo(new Point(12, 7));
        context.EndFigure(isClosed: false);
        context.BeginFigure(new Point(12, 17), isFilled: false);
        context.LineTo(new Point(12, 21));
        context.EndFigure(isClosed: false);
        context.BeginFigure(new Point(3, 12), isFilled: false);
        context.LineTo(new Point(7, 12));
        context.EndFigure(isClosed: false);
        context.BeginFigure(new Point(17, 12), isFilled: false);
        context.LineTo(new Point(21, 12));
        context.EndFigure(isClosed: false);
        context.BeginFigure(new Point(12, 7), isFilled: false);
        context.LineTo(new Point(17, 12));
        context.LineTo(new Point(12, 17));
        context.LineTo(new Point(7, 12));
        context.LineTo(new Point(12, 7));
        context.EndFigure(isClosed: false);
        return geometry;
    }

    private static StreamGeometry BuildDeliveryGeometry()
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(new Point(4, 8), isFilled: false);
        context.LineTo(new Point(19, 8));
        context.LineTo(new Point(16, 5));
        context.EndFigure(isClosed: false);
        context.BeginFigure(new Point(20, 16), isFilled: false);
        context.LineTo(new Point(5, 16));
        context.LineTo(new Point(8, 19));
        context.EndFigure(isClosed: false);
        return geometry;
    }

    private static StreamGeometry BuildCapabilityGeometry()
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(new Point(12, 4), isFilled: false);
        context.LineTo(new Point(20, 12));
        context.LineTo(new Point(12, 20));
        context.LineTo(new Point(4, 12));
        context.LineTo(new Point(12, 4));
        context.EndFigure(isClosed: false);
        context.BeginFigure(new Point(9, 12), isFilled: false);
        context.LineTo(new Point(15, 12));
        context.EndFigure(isClosed: false);
        return geometry;
    }
}
