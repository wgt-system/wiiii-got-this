using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SigilPath = Avalonia.Controls.Shapes.Path;

namespace WiiiiGotThis.Presentation;

internal static class ServiceSigilFactory
{
    public static Grid Create(string serviceName, double size)
    {
        var path = new SigilPath
        {
            Width = size,
            Height = size,
            Stretch = Stretch.Uniform,
            StrokeThickness = 2.2,
            Data = BuildGeometry(serviceName),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            IsHitTestVisible = false
        };
        path.Classes.Add("wgt-service-sigil");
        path.Classes.Add($"sigil-{serviceName.ToLowerInvariant()}");

        return new Grid
        {
            Width = size,
            Height = size,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Children = { path }
        };
    }

    private static StreamGeometry BuildGeometry(string serviceName)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();

        switch (serviceName)
        {
            case "Vocation":
                context.BeginFigure(new Point(7, 32), isFilled: false);
                context.LineTo(new Point(15, 24));
                context.LineTo(new Point(22, 28));
                context.LineTo(new Point(35, 14));
                context.EndFigure(isClosed: false);
                context.BeginFigure(new Point(27, 14), isFilled: false);
                context.LineTo(new Point(35, 14));
                context.LineTo(new Point(35, 22));
                context.EndFigure(isClosed: false);
                break;

            case "Illumination":
                context.BeginFigure(new Point(22, 5), isFilled: false);
                context.LineTo(new Point(25, 17));
                context.LineTo(new Point(37, 22));
                context.LineTo(new Point(25, 27));
                context.LineTo(new Point(22, 39));
                context.LineTo(new Point(19, 27));
                context.LineTo(new Point(7, 22));
                context.LineTo(new Point(19, 17));
                context.LineTo(new Point(22, 5));
                context.EndFigure(isClosed: false);
                break;

            case "Orientation":
                context.BeginFigure(new Point(22, 5), isFilled: false);
                context.LineTo(new Point(22, 12));
                context.EndFigure(isClosed: false);
                context.BeginFigure(new Point(22, 32), isFilled: false);
                context.LineTo(new Point(22, 39));
                context.EndFigure(isClosed: false);
                context.BeginFigure(new Point(5, 22), isFilled: false);
                context.LineTo(new Point(12, 22));
                context.EndFigure(isClosed: false);
                context.BeginFigure(new Point(32, 22), isFilled: false);
                context.LineTo(new Point(39, 22));
                context.EndFigure(isClosed: false);
                context.BeginFigure(new Point(22, 12), isFilled: false);
                context.LineTo(new Point(32, 22));
                context.LineTo(new Point(22, 32));
                context.LineTo(new Point(12, 22));
                context.LineTo(new Point(22, 12));
                context.EndFigure(isClosed: false);
                break;

            case "Conveyance":
                context.BeginFigure(new Point(8, 16), isFilled: false);
                context.LineTo(new Point(34, 16));
                context.LineTo(new Point(29, 11));
                context.EndFigure(isClosed: false);
                context.BeginFigure(new Point(36, 28), isFilled: false);
                context.LineTo(new Point(10, 28));
                context.LineTo(new Point(15, 33));
                context.EndFigure(isClosed: false);
                break;

            default:
                context.BeginFigure(new Point(8, 22), isFilled: false);
                context.LineTo(new Point(36, 22));
                context.EndFigure(isClosed: false);
                context.BeginFigure(new Point(22, 8), isFilled: false);
                context.LineTo(new Point(22, 36));
                context.EndFigure(isClosed: false);
                break;
        }

        return geometry;
    }
}
