using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace WiiiiGotThis.Presentation;

/// <summary>
/// Authored built-environment accents for the three current first-class Product Provider
/// regions in World. The base renderer still owns semantic nodes, hit testing, settlement
/// layout and generic buildings; this non-interactive layer gives each known region a
/// recognisable architectural silhouette without inventing provider capabilities.
/// </summary>
public sealed class AtlasWorldRegionalArchitectureOverlay : Control
{
    private const double WorldCenterX = 1000d;
    private const double WorldCenterY = 660d;
    private const double RegionalDetailRevealZoom = 0.68d;

    private static readonly Point VocationCenter = new(-410, 80);
    private static readonly Point IlluminationCenter = new(-115, -350);
    private static readonly Point OrientationCenter = new(405, -28);

    private static readonly Color VocationAccent = Color.Parse("#55D3A2");
    private static readonly Color IlluminationAccent = Color.Parse("#E3BF6C");
    private static readonly Color OrientationAccent = Color.Parse("#6DB8E7");
    private static readonly Color Shadow = Color.Parse("#010302");
    private static readonly Color WarmWindow = Color.Parse("#FFD58A");
    private static readonly Color CoolWindow = Color.Parse("#8EE8DE");
    private static readonly Color Stone = Color.Parse("#29312D");

    private IReadOnlyList<AtlasNodePresentationViewModel> nodes = Array.Empty<AtlasNodePresentationViewModel>();
    private string? selectedNodeId;
    private double zoom = 0.82d;
    private double translateX;
    private double translateY;

    public AtlasWorldRegionalArchitectureOverlay()
    {
        IsHitTestVisible = false;
        ClipToBounds = true;
    }

    public void SetScene(
        IReadOnlyList<AtlasNodePresentationViewModel> nextNodes,
        string? nextSelectedNodeId)
    {
        nodes = nextNodes;
        selectedNodeId = nextSelectedNodeId;
        InvalidateVisual();
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
        if (zoom < RegionalDetailRevealZoom || nodes.Count == 0)
            return;

        var selected = nodes.FirstOrDefault(node =>
            string.Equals(node.NodeId, selectedNodeId, StringComparison.Ordinal));
        var selectedServiceId = selected?.ServiceIdentity?.Value;

        DrawVocationQuarter(context, FindService("vocation"), selectedServiceId);
        DrawIlluminationCampus(context, FindService("illumination"), selectedServiceId);
        DrawOrientationSurveyDistrict(context, FindService("orientation"), selectedServiceId);
    }

    private AtlasNodePresentationViewModel? FindService(string serviceId) =>
        nodes.FirstOrDefault(node =>
            node.IsService
            && string.Equals(node.ServiceIdentity?.Value, serviceId, StringComparison.Ordinal));

    private void DrawVocationQuarter(
        DrawingContext context,
        AtlasNodePresentationViewModel? service,
        string? selectedServiceId)
    {
        if (service is null)
            return;

        var selected = string.Equals(selectedServiceId, "vocation", StringComparison.Ordinal);
        DrawRegionalFocus(context, VocationCenter, VocationAccent, selected);

        // A denser civic/work quarter: long hall + office slab + low courtyard wing.
        DrawExtrudedBlock(
            context,
            VocationCenter + new Vector(-2, -15),
            78,
            28,
            25,
            VocationAccent,
            service.IsAvailable,
            selected,
            WarmWindow);
        DrawExtrudedBlock(
            context,
            VocationCenter + new Vector(-42, -39),
            30,
            24,
            49,
            VocationAccent,
            service.IsAvailable,
            selected,
            CoolWindow);
        DrawExtrudedBlock(
            context,
            VocationCenter + new Vector(49, 17),
            42,
            26,
            18,
            VocationAccent,
            service.IsAvailable,
            selected,
            WarmWindow);

        if (zoom >= 0.86)
        {
            DrawCourtyard(
                context,
                VocationCenter + new Vector(-11, 36),
                70,
                31,
                VocationAccent,
                58);
            DrawCanopy(
                context,
                VocationCenter + new Vector(-11, 13),
                58,
                VocationAccent,
                service.IsAvailable);
        }
    }

    private void DrawIlluminationCampus(
        DrawingContext context,
        AtlasNodePresentationViewModel? service,
        string? selectedServiceId)
    {
        if (service is null)
            return;

        var selected = string.Equals(selectedServiceId, "illumination", StringComparison.Ordinal);
        DrawRegionalFocus(context, IlluminationCenter, IlluminationAccent, selected);

        // A terraced campus/library silhouette: paired reading wings around a taller lantern hall.
        DrawExtrudedBlock(
            context,
            IlluminationCenter + new Vector(-42, 10),
            38,
            28,
            18,
            IlluminationAccent,
            service.IsAvailable,
            selected,
            WarmWindow);
        DrawExtrudedBlock(
            context,
            IlluminationCenter + new Vector(43, 10),
            38,
            28,
            18,
            IlluminationAccent,
            service.IsAvailable,
            selected,
            WarmWindow);
        DrawExtrudedBlock(
            context,
            IlluminationCenter + new Vector(0, -23),
            46,
            34,
            42,
            IlluminationAccent,
            service.IsAvailable,
            selected,
            WarmWindow);
        DrawExtrudedBlock(
            context,
            IlluminationCenter + new Vector(0, -48),
            24,
            22,
            57,
            IlluminationAccent,
            service.IsAvailable,
            selected,
            WarmWindow);

        DrawLanternCrown(
            context,
            IlluminationCenter + new Vector(10, -108),
            IlluminationAccent,
            service.IsAvailable,
            selected);

        if (zoom >= 0.86)
        {
            DrawCourtyard(
                context,
                IlluminationCenter + new Vector(0, 43),
                82,
                34,
                IlluminationAccent,
                48);
            DrawReadingArcade(context, IlluminationCenter + new Vector(0, 27), IlluminationAccent);
        }
    }

    private void DrawOrientationSurveyDistrict(
        DrawingContext context,
        AtlasNodePresentationViewModel? service,
        string? selectedServiceId)
    {
        if (service is null)
            return;

        var selected = string.Equals(selectedServiceId, "orientation", StringComparison.Ordinal);
        DrawRegionalFocus(context, OrientationCenter, OrientationAccent, selected);

        // Lower angular survey pavilion + tall observation tower create a different skyline.
        DrawExtrudedBlock(
            context,
            OrientationCenter + new Vector(-17, -3),
            60,
            30,
            22,
            OrientationAccent,
            service.IsAvailable,
            selected,
            CoolWindow);
        DrawExtrudedBlock(
            context,
            OrientationCenter + new Vector(34, -36),
            22,
            22,
            62,
            OrientationAccent,
            service.IsAvailable,
            selected,
            CoolWindow);
        DrawExtrudedBlock(
            context,
            OrientationCenter + new Vector(-48, 24),
            34,
            23,
            15,
            OrientationAccent,
            service.IsAvailable,
            selected,
            CoolWindow);

        DrawSurveyMast(
            context,
            OrientationCenter + new Vector(45, -104),
            OrientationAccent,
            service.IsAvailable,
            selected);

        if (zoom >= 0.86)
        {
            DrawSurveyDeck(
                context,
                OrientationCenter + new Vector(29, -73),
                OrientationAccent,
                service.IsAvailable);
            DrawOrientationBearingMarks(context, OrientationCenter + new Vector(-13, 40), OrientationAccent);
        }
    }

    private void DrawRegionalFocus(DrawingContext context, Point center, Color accent, bool selected)
    {
        if (!selected)
            return;

        var screen = ScreenPoint(center);
        context.DrawEllipse(
            new SolidColorBrush(WithAlpha(accent, 34)),
            null,
            screen,
            118 * zoom,
            74 * zoom);
        context.DrawEllipse(
            null,
            new Pen(new SolidColorBrush(WithAlpha(accent, 82)), Math.Max(0.8, 1.1 * zoom)),
            screen,
            94 * zoom,
            56 * zoom);
    }

    private void DrawExtrudedBlock(
        DrawingContext context,
        Point center,
        double width,
        double depth,
        double height,
        Color accent,
        bool available,
        bool selected,
        Color windowColor)
    {
        var halfWidth = width / 2;
        var halfDepth = depth / 2;
        var lift = new Vector(height * 0.18, -height);

        var backLeft = center + new Vector(-halfWidth, -halfDepth);
        var backRight = center + new Vector(halfWidth, -halfDepth);
        var frontRight = center + new Vector(halfWidth, halfDepth);
        var frontLeft = center + new Vector(-halfWidth, halfDepth);

        var shadowOffset = new Vector(10, 8);
        context.DrawGeometry(
            new SolidColorBrush(WithAlpha(Shadow, 105)),
            null,
            PolygonWorld(
            [
                backLeft + shadowOffset,
                backRight + shadowOffset,
                frontRight + shadowOffset,
                frontLeft + shadowOffset
            ]));

        var alpha = available ? (byte)238 : (byte)150;
        var front = PolygonWorld(
        [
            frontLeft,
            frontRight,
            frontRight + lift,
            frontLeft + lift
        ]);
        var side = PolygonWorld(
        [
            backRight,
            frontRight,
            frontRight + lift,
            backRight + lift
        ]);
        var roof = PolygonWorld(
        [
            backLeft + lift,
            backRight + lift,
            frontRight + lift,
            frontLeft + lift
        ]);

        context.DrawGeometry(
            new SolidColorBrush(WithAlpha(Mix(Stone, accent, 0.20), alpha)),
            null,
            front);
        context.DrawGeometry(
            new SolidColorBrush(WithAlpha(Mix(Stone, accent, 0.10), alpha)),
            null,
            side);
        context.DrawGeometry(
            new SolidColorBrush(WithAlpha(Mix(Stone, accent, selected ? 0.34 : 0.25), alpha)),
            new Pen(new SolidColorBrush(WithAlpha(accent, selected ? (byte)120 : (byte)62)), Math.Max(0.55, 0.75 * zoom)),
            roof);

        if (zoom < 0.76 || !available)
            return;

        var windowPen = new Pen(
            new SolidColorBrush(WithAlpha(windowColor, selected ? (byte)205 : (byte)142)),
            Math.Max(0.8, 1.1 * zoom));
        var windowY = center.Y + halfDepth - height * 0.44;
        for (var index = -1; index <= 1; index++)
        {
            var x = center.X + index * width * 0.21 + lift.X * 0.55;
            context.DrawLine(
                windowPen,
                ScreenPoint(new Point(x, windowY)),
                ScreenPoint(new Point(x + 4, windowY - 7)));
        }
    }

    private void DrawCourtyard(
        DrawingContext context,
        Point center,
        double width,
        double height,
        Color accent,
        byte alpha)
    {
        var rect = ScreenRect(center, width, height);
        context.DrawRectangle(
            new SolidColorBrush(WithAlpha(Mix(Stone, accent, 0.08), 76)),
            new Pen(new SolidColorBrush(WithAlpha(accent, alpha)), Math.Max(0.55, 0.8 * zoom)),
            rect);
    }

    private void DrawCanopy(
        DrawingContext context,
        Point center,
        double width,
        Color accent,
        bool available)
    {
        var pen = new Pen(
            new SolidColorBrush(WithAlpha(accent, available ? (byte)116 : (byte)54)),
            Math.Max(1.1, 1.5 * zoom));
        context.DrawLine(
            pen,
            ScreenPoint(center + new Vector(-width / 2, 0)),
            ScreenPoint(center + new Vector(width / 2, 0)));
        context.DrawLine(
            pen,
            ScreenPoint(center + new Vector(-width / 2 + 7, 0)),
            ScreenPoint(center + new Vector(-width / 2 + 7, 12)));
        context.DrawLine(
            pen,
            ScreenPoint(center + new Vector(width / 2 - 7, 0)),
            ScreenPoint(center + new Vector(width / 2 - 7, 12)));
    }

    private void DrawLanternCrown(
        DrawingContext context,
        Point center,
        Color accent,
        bool available,
        bool selected)
    {
        var point = ScreenPoint(center);
        var radius = 10 * zoom;
        context.DrawEllipse(
            new SolidColorBrush(WithAlpha(accent, available ? (byte)70 : (byte)24)),
            new Pen(new SolidColorBrush(WithAlpha(accent, selected ? (byte)220 : (byte)145)), Math.Max(0.8, 1.1 * zoom)),
            point,
            radius,
            radius * 0.72);
        context.DrawLine(
            new Pen(new SolidColorBrush(WithAlpha(accent, available ? (byte)170 : (byte)62)), Math.Max(0.65, 0.9 * zoom)),
            new Point(point.X, point.Y - radius * 0.75),
            new Point(point.X, point.Y - radius * 2.2));
    }

    private void DrawReadingArcade(DrawingContext context, Point center, Color accent)
    {
        var pen = new Pen(new SolidColorBrush(WithAlpha(accent, 66)), Math.Max(0.55, 0.8 * zoom));
        for (var index = -3; index <= 3; index++)
        {
            var x = center.X + index * 12;
            context.DrawLine(
                pen,
                ScreenPoint(new Point(x, center.Y - 9)),
                ScreenPoint(new Point(x, center.Y + 9)));
        }
        context.DrawLine(
            pen,
            ScreenPoint(center + new Vector(-42, -9)),
            ScreenPoint(center + new Vector(42, -9)));
    }

    private void DrawSurveyMast(
        DrawingContext context,
        Point basePoint,
        Color accent,
        bool available,
        bool selected)
    {
        var baseScreen = ScreenPoint(basePoint);
        var topScreen = ScreenPoint(basePoint + new Vector(0, -42));
        var pen = new Pen(
            new SolidColorBrush(WithAlpha(accent, available ? (byte)190 : (byte)72)),
            Math.Max(0.8, 1.15 * zoom));
        context.DrawLine(pen, baseScreen, topScreen);
        context.DrawLine(
            pen,
            ScreenPoint(basePoint + new Vector(-8, -24)),
            topScreen);
        context.DrawLine(
            pen,
            ScreenPoint(basePoint + new Vector(8, -24)),
            topScreen);

        context.DrawEllipse(
            new SolidColorBrush(WithAlpha(accent, available ? (byte)38 : (byte)14)),
            new Pen(new SolidColorBrush(WithAlpha(accent, selected ? (byte)220 : (byte)130)), Math.Max(0.7, 1 * zoom)),
            topScreen,
            8 * zoom,
            5 * zoom);
    }

    private void DrawSurveyDeck(
        DrawingContext context,
        Point center,
        Color accent,
        bool available)
    {
        var rect = ScreenRect(center, 32, 10);
        context.DrawRectangle(
            new SolidColorBrush(WithAlpha(Mix(Stone, accent, 0.22), available ? (byte)220 : (byte)126)),
            new Pen(new SolidColorBrush(WithAlpha(accent, 98)), Math.Max(0.55, 0.8 * zoom)),
            rect);
    }

    private void DrawOrientationBearingMarks(DrawingContext context, Point center, Color accent)
    {
        var point = ScreenPoint(center);
        var pen = new Pen(new SolidColorBrush(WithAlpha(accent, 66)), Math.Max(0.5, 0.7 * zoom));
        var radius = 16 * zoom;
        context.DrawEllipse(null, pen, point, radius, radius * 0.64);
        context.DrawLine(pen, new Point(point.X - radius, point.Y), new Point(point.X + radius, point.Y));
        context.DrawLine(pen, new Point(point.X, point.Y - radius * 0.64), new Point(point.X, point.Y + radius * 0.64));
    }

    private StreamGeometry PolygonWorld(Point[] points)
    {
        var geometry = new StreamGeometry();
        using var gc = geometry.Open();
        gc.BeginFigure(ScreenPoint(points[0]), isFilled: true);
        for (var index = 1; index < points.Length; index++)
            gc.LineTo(ScreenPoint(points[index]), isStroked: true);
        gc.EndFigure(isClosed: true);
        return geometry;
    }

    private Rect ScreenRect(Point center, double width, double height)
    {
        var screen = ScreenPoint(center);
        return new Rect(
            screen.X - width * zoom / 2,
            screen.Y - height * zoom / 2,
            width * zoom,
            height * zoom);
    }

    private Point ScreenPoint(Point worldPoint) => new(
        (WorldCenterX + worldPoint.X) * zoom + translateX,
        (WorldCenterY + worldPoint.Y) * zoom + translateY);

    private static Color Mix(Color from, Color to, double t) => Color.FromArgb(
        (byte)Math.Round(from.A + (to.A - from.A) * t),
        (byte)Math.Round(from.R + (to.R - from.R) * t),
        (byte)Math.Round(from.G + (to.G - from.G) * t),
        (byte)Math.Round(from.B + (to.B - from.B) * t));

    private static Color WithAlpha(Color color, byte alpha) =>
        Color.FromArgb(alpha, color.R, color.G, color.B);
}
