using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SigilPath = Avalonia.Controls.Shapes.Path;

namespace WiiiiGotThis.Presentation;

internal static class CoreSigilFactory
{
    public static Grid Create(double size)
    {
        var canvasSize = 48d;
        var canvas = new Canvas
        {
            Width = canvasSize,
            Height = canvasSize,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            IsHitTestVisible = false
        };

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            // Four explicit composition directions converge on a bounded central hub.
            context.BeginFigure(new Point(24, 3), isFilled: false);
            context.LineTo(new Point(24, 14));
            context.EndFigure(isClosed: false);
            context.BeginFigure(new Point(45, 24), isFilled: false);
            context.LineTo(new Point(34, 24));
            context.EndFigure(isClosed: false);
            context.BeginFigure(new Point(24, 45), isFilled: false);
            context.LineTo(new Point(24, 34));
            context.EndFigure(isClosed: false);
            context.BeginFigure(new Point(3, 24), isFilled: false);
            context.LineTo(new Point(14, 24));
            context.EndFigure(isClosed: false);

            context.BeginFigure(new Point(24, 13), isFilled: false);
            context.LineTo(new Point(35, 24));
            context.LineTo(new Point(24, 35));
            context.LineTo(new Point(13, 24));
            context.LineTo(new Point(24, 13));
            context.EndFigure(isClosed: false);

            context.BeginFigure(new Point(24, 18), isFilled: false);
            context.LineTo(new Point(30, 24));
            context.LineTo(new Point(24, 30));
            context.LineTo(new Point(18, 24));
            context.LineTo(new Point(24, 18));
            context.EndFigure(isClosed: false);
        }

        var path = new SigilPath
        {
            Width = canvasSize,
            Height = canvasSize,
            Stretch = Stretch.Uniform,
            StrokeThickness = 2.1,
            Data = geometry,
            IsHitTestVisible = false
        };
        path.Classes.Add("wgt-core-sigil");
        canvas.Children.Add(path);

        foreach (var point in new[]
                 {
                     new Point(21.5, 0.5),
                     new Point(42.5, 21.5),
                     new Point(21.5, 42.5),
                     new Point(0.5, 21.5)
                 })
        {
            var port = new Border
            {
                Width = 5,
                Height = 5,
                CornerRadius = new CornerRadius(3),
                IsHitTestVisible = false
            };
            port.Classes.Add("wgt-core-sigil-port");
            Canvas.SetLeft(port, point.X);
            Canvas.SetTop(port, point.Y);
            canvas.Children.Add(port);
        }

        var frame = new Border
        {
            Width = size,
            Height = size,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Child = canvas
        };
        frame.Classes.Add("wgt-core-sigil-frame");
        return new Grid
        {
            Width = size,
            Height = size,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Children = { frame }
        };
    }
}
