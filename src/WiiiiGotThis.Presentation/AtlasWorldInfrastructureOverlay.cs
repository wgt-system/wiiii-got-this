using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

/// <summary>
/// World-only presentation of shared capability consumption.
///
/// A shared provider does not become another product settlement merely because a product
/// consumes it. Instead, the consuming product receives a local facility/attachment and a
/// restrained infrastructure route to the shared provider backbone. This is presentation
/// semantics only; it does not move capability ownership into the consuming product.
/// </summary>
public sealed class AtlasWorldInfrastructureOverlay : Control
{
    private const double WorldCenterX = 1000d;
    private const double WorldCenterY = 660d;

    private static readonly Color DataLine = Color.Parse("#7398B5");
    private static readonly Color DataGlow = Color.Parse("#80D7C0");
    private static readonly Color FacilityBody = Color.Parse("#182922");
    private static readonly Color FacilityRoof = Color.Parse("#26473B");
    private static readonly Color FacilityDark = Color.Parse("#07100D");
    private static readonly Color WarmLight = Color.Parse("#FFD58A");
    private static readonly Color Text = Color.Parse("#BBD1C6");

    private IReadOnlyList<AtlasNodePresentationViewModel> nodes = Array.Empty<AtlasNodePresentationViewModel>();
    private IReadOnlyList<AtlasConnectionPresentationViewModel> connections = Array.Empty<AtlasConnectionPresentationViewModel>();
    private string? selectedNodeId;
    private bool reducedMotion;
    private double zoom = 0.82d;
    private double translateX;
    private double translateY;

    public AtlasWorldInfrastructureOverlay()
    {
        IsHitTestVisible = false;
        ClipToBounds = true;
    }

    public void SetScene(
        IReadOnlyList<AtlasNodePresentationViewModel> nextNodes,
        IReadOnlyList<AtlasConnectionPresentationViewModel> nextConnections,
        string? nextSelectedNodeId,
        bool nextReducedMotion)
    {
        nodes = nextNodes;
        connections = nextConnections;
        selectedNodeId = nextSelectedNodeId;
        reducedMotion = nextReducedMotion;
        InvalidateVisual();
        RequestFrameIfNeeded();
    }

    public void SetCamera(double nextZoom, double nextTranslateX, double nextTranslateY)
    {
        zoom = nextZoom;
        translateX = nextTranslateX;
        translateY = nextTranslateY;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        foreach (var connection in connections.Where(item => item.IsCapabilityUse && item.IsEnabled))
        {
            if (!connection.Source.IsService || !connection.Target.IsCapability)
                continue;

            var provider = nodes.FirstOrDefault(node =>
                node.IsService
                && node.ServiceIdentity == connection.Target.ServiceIdentity);
            if (provider?.IsSharedCapabilityProvider != true)
                continue;

            if (!TryProductWorldPosition(connection.Source.ServiceIdentity?.Value, out var productCenter))
                continue;

            var facility = LocalFacilityPosition(connection.Source.ServiceIdentity?.Value, productCenter);
            var backbone = SharedBackbonePosition(provider.ServiceIdentity?.Value);
            var focused = string.Equals(selectedNodeId, connection.Source.NodeId, StringComparison.Ordinal)
                || string.Equals(selectedNodeId, connection.Target.NodeId, StringComparison.Ordinal)
                || string.Equals(selectedNodeId, provider.NodeId, StringComparison.Ordinal);
            var powered = connection.Target.IsAvailable;

            DrawInfrastructureRoute(context, facility, backbone, powered, focused);
            DrawLocalFacility(
                context,
                facility,
                connection.Source.Title,
                connection.Target.Title,
                powered,
                focused);
        }

        RequestFrameIfNeeded();
    }

    private void DrawInfrastructureRoute(
        DrawingContext context,
        Point facility,
        Point backbone,
        bool powered,
        bool focused)
    {
        var delta = backbone - facility;
        var control = new Point(
            facility.X + delta.X * 0.52,
            facility.Y + delta.Y * 0.36 - Math.Min(72, Math.Abs(delta.X) * 0.08));
        var geometry = Quadratic(facility, control, backbone);
        var baseAlpha = powered ? (byte)(focused ? 150 : 82) : (byte)42;
        var lineAlpha = powered ? (byte)(focused ? 220 : 126) : (byte)68;

        context.DrawGeometry(
            null,
            new Pen(new SolidColorBrush(WithAlpha(DataGlow, baseAlpha)), Math.Max(4, 6 * zoom)),
            geometry);
        context.DrawGeometry(
            null,
            new Pen(new SolidColorBrush(WithAlpha(DataLine, lineAlpha)), Math.Max(0.9, 1.3 * zoom)),
            geometry);

        if (powered && focused && !reducedMotion)
        {
            var phase = DateTime.UtcNow.TimeOfDay.TotalSeconds * 0.10 % 1d;
            for (var index = 0; index < 3; index++)
            {
                var t = (phase + index / 3d) % 1d;
                var point = ScreenPoint(QuadraticPoint(facility, control, backbone, t));
                context.DrawEllipse(
                    new SolidColorBrush(WithAlpha(DataGlow, 225)),
                    null,
                    point,
                    2.3 * zoom,
                    2.3 * zoom);
            }
        }
    }

    private void DrawLocalFacility(
        DrawingContext context,
        Point center,
        string productName,
        string capabilityName,
        bool powered,
        bool focused)
    {
        var screen = ScreenPoint(center);
        var scale = zoom;

        if (focused || powered)
        {
            var glow = new RadialGradientBrush
            {
                Center = RelativePoint.Center,
                GradientOrigin = RelativePoint.Center
            };
            glow.GradientStops.Add(new GradientStop(WithAlpha(powered ? DataGlow : DataLine, focused ? (byte)62 : (byte)32), 0));
            glow.GradientStops.Add(new GradientStop(WithAlpha(DataGlow, 0), 1));
            context.DrawEllipse(glow, null, screen, 58 * scale, 42 * scale);
        }

        // A compact relay/data works inside the consuming product region. It deliberately
        // differs from the larger shared Conveyance yard: this is the product's attachment,
        // not another copy of the provider/runtime.
        DrawWarehouse(context, center + new Vector(-17, 7), 42, 24, powered);
        DrawWarehouse(context, center + new Vector(24, 12), 30, 19, powered);
        DrawRelayMast(context, center + new Vector(24, -17), powered);

        if (zoom >= 0.72)
        {
            DrawCenteredText(
                context,
                capabilityName.Equals("Cross-device delivery", StringComparison.OrdinalIgnoreCase) ? "SYNC RELAY" : capabilityName.ToUpperInvariant(),
                new Point(screen.X, screen.Y + 27 * scale),
                Math.Clamp(6.8 * scale, 5.8, 8.2),
                WithAlpha(Text, focused ? (byte)220 : (byte)150));
            if (focused && zoom >= 0.92)
            {
                DrawCenteredText(
                    context,
                    productName,
                    new Point(screen.X, screen.Y + 39 * scale),
                    Math.Clamp(6.1 * scale, 5.5, 7.4),
                    WithAlpha(DataGlow, 160));
            }
        }
    }

    private void DrawWarehouse(DrawingContext context, Point baseWorld, double width, double height, bool powered)
    {
        var basePoint = ScreenPoint(baseWorld);
        var w = width * zoom;
        var h = height * zoom;
        var depth = 8 * zoom;
        var left = basePoint.X - w / 2;
        var right = basePoint.X + w / 2;
        var top = basePoint.Y - h;

        var front = Polygon([
            new(left, top), new(right, top), new(right, basePoint.Y), new(left, basePoint.Y)
        ]);
        context.DrawGeometry(
            new SolidColorBrush(WithAlpha(FacilityBody, 245)),
            new Pen(new SolidColorBrush(WithAlpha(DataLine, 88)), 0.7),
            front);

        var roof = Polygon([
            new(left, top), new(left + depth, top - depth * 0.5),
            new(right + depth, top - depth * 0.5), new(right, top)
        ]);
        context.DrawGeometry(new SolidColorBrush(WithAlpha(FacilityRoof, 248)), null, roof);

        var side = Polygon([
            new(right, top), new(right + depth, top - depth * 0.5),
            new(right + depth, basePoint.Y - depth * 0.5), new(right, basePoint.Y)
        ]);
        context.DrawGeometry(new SolidColorBrush(WithAlpha(FacilityDark, 248)), null, side);

        var door = new Rect(
            basePoint.X - 5 * zoom,
            basePoint.Y - 10 * zoom,
            10 * zoom,
            10 * zoom);
        context.FillRectangle(new SolidColorBrush(WithAlpha(FacilityDark, 245)), door);
        if (powered)
        {
            context.FillRectangle(
                new SolidColorBrush(WithAlpha(WarmLight, 210)),
                new Rect(left + 6 * zoom, top + 7 * zoom, 4 * zoom, 2.2 * zoom));
        }
    }

    private void DrawRelayMast(DrawingContext context, Point worldPoint, bool powered)
    {
        var point = ScreenPoint(worldPoint);
        var line = new Pen(new SolidColorBrush(WithAlpha(DataLine, powered ? (byte)205 : (byte)92)), Math.Max(0.7, zoom));
        context.DrawLine(line, new Point(point.X, point.Y - 31 * zoom), new Point(point.X - 8 * zoom, point.Y));
        context.DrawLine(line, new Point(point.X, point.Y - 31 * zoom), new Point(point.X + 8 * zoom, point.Y));
        context.DrawLine(line, new Point(point.X - 6 * zoom, point.Y - 11 * zoom), new Point(point.X + 6 * zoom, point.Y - 11 * zoom));
        context.DrawLine(line, new Point(point.X - 4 * zoom, point.Y - 21 * zoom), new Point(point.X + 4 * zoom, point.Y - 21 * zoom));
        if (powered)
            context.DrawEllipse(new SolidColorBrush(WithAlpha(DataGlow, 235)), null, new Point(point.X, point.Y - 33 * zoom), 1.8 * zoom, 1.8 * zoom);
    }

    private static Point LocalFacilityPosition(string? serviceId, Point productCenter) => serviceId switch
    {
        "vocation" => productCenter + new Vector(92, 73),
        "illumination" => productCenter + new Vector(98, 78),
        "orientation" => productCenter + new Vector(-92, 82),
        _ => productCenter + new Vector(84, 72)
    };

    private static bool TryProductWorldPosition(string? serviceId, out Point position)
    {
        position = serviceId switch
        {
            "vocation" => new Point(-410, 80),
            "illumination" => new Point(-115, -350),
            "orientation" => new Point(405, -28),
            _ => default
        };
        return serviceId is "vocation" or "illumination" or "orientation";
    }

    private static Point SharedBackbonePosition(string? serviceId) => serviceId switch
    {
        "conveyance" => new Point(190, 360),
        _ => new Point(250, 330)
    };

    private StreamGeometry Quadratic(Point start, Point control, Point end)
    {
        var geometry = new StreamGeometry();
        using var geometryContext = geometry.Open();
        geometryContext.BeginFigure(ScreenPoint(start), isFilled: false);
        geometryContext.QuadraticBezierTo(ScreenPoint(control), ScreenPoint(end), isStroked: true);
        geometryContext.EndFigure(isClosed: false);
        return geometry;
    }

    private static StreamGeometry Polygon(IReadOnlyList<Point> points)
    {
        var geometry = new StreamGeometry();
        using var geometryContext = geometry.Open();
        geometryContext.BeginFigure(points[0], isFilled: true);
        for (var index = 1; index < points.Count; index++)
            geometryContext.LineTo(points[index], isStroked: true);
        geometryContext.EndFigure(isClosed: true);
        return geometry;
    }

    private Point ScreenPoint(Point worldPoint) => new(
        (WorldCenterX + worldPoint.X) * zoom + translateX,
        (WorldCenterY + worldPoint.Y) * zoom + translateY);

    private static Point QuadraticPoint(Point start, Point control, Point end, double t)
    {
        var inverse = 1 - t;
        return new Point(
            inverse * inverse * start.X + 2 * inverse * t * control.X + t * t * end.X,
            inverse * inverse * start.Y + 2 * inverse * t * control.Y + t * t * end.Y);
    }

    private void RequestFrameIfNeeded()
    {
        if (reducedMotion || selectedNodeId is null)
            return;
        TopLevel.GetTopLevel(this)?.RequestAnimationFrame(_ => InvalidateVisual());
    }

    private static void DrawCenteredText(DrawingContext context, string text, Point topCenter, double fontSize, Color color)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            fontSize,
            new SolidColorBrush(color));
        context.DrawText(formatted, new Point(topCenter.X - formatted.Width / 2, topCenter.Y));
    }

    private static Color WithAlpha(Color color, byte alpha) => Color.FromArgb(alpha, color.R, color.G, color.B);
}
