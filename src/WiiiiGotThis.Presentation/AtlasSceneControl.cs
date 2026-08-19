using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

/// <summary>
/// Draws the Atlas as one spatial scene instead of materializing a control tree for
/// graph primitives. WGT chrome and provider Product Surfaces remain normal controls;
/// this control owns the authored Atlas landscape, scene rendering and scene hit testing.
/// </summary>
public sealed class AtlasSceneControl : Control
{
    private const double WorldCenterX = 1000d;
    private const double WorldCenterY = 660d;
    private static readonly TimeSpan ThemeTransitionDuration = TimeSpan.FromMilliseconds(220);

    private static readonly Point[] CoreContour =
    [
        new(-1.00, -0.12), new(-0.72, -0.78), new(-0.10, -0.96), new(0.66, -0.70),
        new(1.00, -0.06), new(0.72, 0.72), new(0.06, 0.96), new(-0.76, 0.66)
    ];

    private static readonly Point[] VocationContour =
    [
        new(-1.00, -0.08), new(-0.80, -0.70), new(-0.24, -0.96), new(0.48, -0.82),
        new(1.00, -0.20), new(0.82, 0.56), new(0.18, 0.94), new(-0.68, 0.70)
    ];

    private static readonly Point[] IlluminationContour =
    [
        new(-0.96, -0.24), new(-0.58, -0.88), new(0.10, -1.00), new(0.80, -0.62),
        new(0.98, 0.04), new(0.64, 0.78), new(-0.08, 0.94), new(-0.78, 0.58)
    ];

    private static readonly Point[] OrientationContour =
    [
        new(-0.98, -0.18), new(-0.70, -0.82), new(-0.02, -0.92), new(0.78, -0.70),
        new(1.00, 0.00), new(0.70, 0.82), new(0.02, 0.96), new(-0.76, 0.64)
    ];

    private static readonly Point[] ConveyanceContour =
    [
        new(-1.00, -0.10), new(-0.72, -0.72), new(-0.06, -0.92), new(0.82, -0.64),
        new(0.98, 0.08), new(0.58, 0.84), new(-0.18, 0.96), new(-0.86, 0.52)
    ];

    private IReadOnlyList<AtlasNodePresentationViewModel> nodes = Array.Empty<AtlasNodePresentationViewModel>();
    private IReadOnlyList<AtlasConnectionPresentationViewModel> connections = Array.Empty<AtlasConnectionPresentationViewModel>();
    private string? selectedNodeId;
    private AtlasThemePreference theme = AtlasThemePreference.Technical;
    private AtlasThemePreference previousTheme = AtlasThemePreference.Technical;
    private DateTime themeTransitionStartedUtc;
    private bool themeTransitionActive;
    private bool reducedMotion;
    private double zoom = 0.82d;
    private double translateX;
    private double translateY;

    public AtlasSceneControl()
    {
        Focusable = true;
        ClipToBounds = true;
    }

    public event Action<AtlasNodePresentationViewModel>? NodeInvoked;
    public event Action<AtlasNodePresentationViewModel>? NodeActivated;

    public void SetScene(
        IReadOnlyList<AtlasNodePresentationViewModel> nextNodes,
        IReadOnlyList<AtlasConnectionPresentationViewModel> nextConnections,
        string? nextSelectedNodeId,
        AtlasThemePreference nextTheme,
        bool nextReducedMotion)
    {
        nodes = nextNodes;
        connections = nextConnections;
        selectedNodeId = nextSelectedNodeId;
        reducedMotion = nextReducedMotion;

        if (nextTheme != theme)
        {
            previousTheme = theme;
            theme = nextTheme;
            themeTransitionStartedUtc = DateTime.UtcNow;
            themeTransitionActive = !reducedMotion;
        }
        else if (reducedMotion)
        {
            themeTransitionActive = false;
            previousTheme = theme;
        }

        InvalidateVisual();
        RequestSceneFrame();
    }

    public void SetCamera(double nextZoom, double nextTranslateX, double nextTranslateY)
    {
        if (Math.Abs(zoom - nextZoom) < 0.0001
            && Math.Abs(translateX - nextTranslateX) < 0.01
            && Math.Abs(translateY - nextTranslateY) < 0.01)
        {
            return;
        }

        zoom = nextZoom;
        translateX = nextTranslateX;
        translateY = nextTranslateY;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var transitionProgress = ThemeTransitionProgress();
        var palette = ScenePalette.Lerp(
            ScenePalette.For(previousTheme),
            ScenePalette.For(theme),
            transitionProgress);
        var focused = AtlasPresentationFocus.Build(connections, selectedNodeId);

        DrawBackdrop(context, palette);
        DrawLandscapeAtmosphere(context, palette, focused);
        DrawRegions(context, palette, focused);
        DrawConnections(context, palette, focused);

        foreach (var node in nodes.Where(node => node.IsCapability))
            DrawCapability(context, node, focused, palette);

        foreach (var node in nodes.Where(node => !node.IsCapability))
            DrawProductNode(context, node, focused, palette);

        DrawVignette(context, palette);
        RequestSceneFrame();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed)
            return;

        var node = HitTestNode(e.GetPosition(this));
        if (node is null)
            return;

        Focus();
        NodeInvoked?.Invoke(node);
        if (e.ClickCount >= 2 && node.CanOpenProductSurface)
            NodeActivated?.Invoke(node);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter
            && selectedNodeId is not null
            && nodes.FirstOrDefault(node => string.Equals(node.NodeId, selectedNodeId, StringComparison.Ordinal)) is { } node)
        {
            if (node.CanOpenProductSurface)
                NodeActivated?.Invoke(node);
            else
                NodeInvoked?.Invoke(node);
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private void DrawBackdrop(DrawingContext context, ScenePalette palette)
    {
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(
            LinearGradient(
                palette.BackgroundTop,
                palette.BackgroundBottom,
                new RelativePoint(0.12, 0.05, RelativeUnit.Relative),
                new RelativePoint(0.90, 0.94, RelativeUnit.Relative)),
            bounds);

        var core = ToScreen(WorldCenterX, WorldCenterY);
        context.DrawEllipse(
            RadialGradient(
                WithAlpha(palette.Core, 42),
                WithAlpha(palette.Core, 0),
                0.62),
            null,
            core,
            520 * zoom,
            390 * zoom);

        switch (theme)
        {
            case AtlasThemePreference.Technical:
                DrawTechnicalField(context, palette);
                break;
            case AtlasThemePreference.Elegant:
                DrawElegantField(context, palette);
                break;
            case AtlasThemePreference.Machine:
                DrawMachineField(context, palette);
                break;
            case AtlasThemePreference.World:
                DrawWorldField(context, palette);
                break;
        }
    }

    private void DrawTechnicalField(DrawingContext context, ScenePalette palette)
    {
        var pen = new Pen(new SolidColorBrush(WithAlpha(palette.Primary, 16)), 1);
        const double spacing = 88d;
        var xOffset = Mod(translateX, spacing * zoom);
        var yOffset = Mod(translateY, spacing * zoom);
        var step = Math.Max(24d, spacing * zoom);

        for (var x = xOffset - step; x <= Bounds.Width + step; x += step)
            context.DrawLine(pen, new Point(x, 0), new Point(x, Bounds.Height));
        for (var y = yOffset - step; y <= Bounds.Height + step; y += step)
            context.DrawLine(pen, new Point(0, y), new Point(Bounds.Width, y));
    }

    private void DrawElegantField(DrawingContext context, ScenePalette palette)
    {
        var center = ToScreen(WorldCenterX, WorldCenterY);
        context.DrawEllipse(
            null,
            new Pen(new SolidColorBrush(WithAlpha(palette.Secondary, 20)), 1),
            center,
            560 * zoom,
            405 * zoom);
        context.DrawEllipse(
            null,
            new Pen(new SolidColorBrush(WithAlpha(palette.Primary, 13)), 1),
            center,
            690 * zoom,
            500 * zoom);
    }

    private void DrawMachineField(DrawingContext context, ScenePalette palette)
    {
        var center = ToScreen(WorldCenterX, WorldCenterY);
        var pen = new Pen(new SolidColorBrush(WithAlpha(palette.Primary, 22)), 1);
        var halfWidth = 610 * zoom;
        var halfHeight = 410 * zoom;
        var frame = new Rect(center.X - halfWidth, center.Y - halfHeight, halfWidth * 2, halfHeight * 2);
        context.DrawRectangle(null, pen, frame);
        DrawMachineCorners(context, frame, 34 * zoom, new Pen(new SolidColorBrush(WithAlpha(palette.Primary, 56)), 1.4));
    }

    private void DrawWorldField(DrawingContext context, ScenePalette palette)
    {
        var center = ToScreen(WorldCenterX, WorldCenterY);
        var pen = new Pen(new SolidColorBrush(WithAlpha(palette.Secondary, 17)), 1);
        context.DrawEllipse(null, pen, new Point(center.X - 110 * zoom, center.Y - 38 * zoom), 610 * zoom, 330 * zoom);
        context.DrawEllipse(null, pen, new Point(center.X + 70 * zoom, center.Y + 72 * zoom), 525 * zoom, 290 * zoom);
    }

    private void DrawLandscapeAtmosphere(
        DrawingContext context,
        ScenePalette palette,
        IReadOnlySet<string> focused)
    {
        foreach (var service in nodes.Where(node => node.IsService))
        {
            var spec = RegionFor(service);
            var accent = AccentFor(service, palette);
            var center = ToScreen(WorldCenterX + spec.Center.X, WorldCenterY + spec.Center.Y);
            var regionFocused = RegionIsFocused(service, focused);
            var alpha = regionFocused ? (byte)58 : service.IsAvailable ? (byte)38 : (byte)24;

            context.DrawEllipse(
                RadialGradient(
                    WithAlpha(accent, alpha),
                    WithAlpha(accent, 0),
                    0.70),
                null,
                center,
                spec.RadiusX * 1.18 * zoom,
                spec.RadiusY * 1.16 * zoom);
        }
    }

    private void DrawRegions(
        DrawingContext context,
        ScenePalette palette,
        IReadOnlySet<string> focused)
    {
        if (nodes.FirstOrDefault(node => node.IsCore) is { } core)
            DrawRegion(context, core, CoreRegion(), palette, focused);

        foreach (var service in nodes.Where(node => node.IsService))
            DrawRegion(context, service, RegionFor(service), palette, focused);
    }

    private void DrawRegion(
        DrawingContext context,
        AtlasNodePresentationViewModel node,
        RegionSpec spec,
        ScenePalette palette,
        IReadOnlySet<string> focused)
    {
        var accent = AccentFor(node, palette);
        var focusedRegion = node.IsCore
            ? selectedNodeId is null || focused.Contains(node.NodeId)
            : RegionIsFocused(node, focused);
        var angular = theme == AtlasThemePreference.Machine;
        var geometry = CreateRegionGeometry(spec, 1d, angular, default);
        var shadowOffset = new Vector(0, Math.Max(3d, 10d * zoom));

        context.DrawGeometry(
            new SolidColorBrush(WithAlpha(palette.Shadow, node.IsAvailable || node.IsCore ? (byte)112 : (byte)84)),
            null,
            CreateRegionGeometry(spec, 1.01d, angular, shadowOffset));

        var top = Mix(palette.RegionSurface, accent, node.IsCore ? 0.24 : 0.16);
        var bottom = Mix(palette.RegionDepth, accent, node.IsAvailable || node.IsCore ? 0.08 : 0.03);
        var fill = LinearGradient(
            WithAlpha(top, focusedRegion ? (byte)242 : (byte)224),
            WithAlpha(bottom, focusedRegion ? (byte)236 : (byte)214),
            new RelativePoint(0.18, 0.08, RelativeUnit.Relative),
            new RelativePoint(0.82, 0.94, RelativeUnit.Relative));

        context.DrawGeometry(
            fill,
            new Pen(
                new SolidColorBrush(WithAlpha(accent, focusedRegion ? (byte)116 : (byte)64)),
                focusedRegion ? 1.5 : 1.0),
            geometry);

        using (context.PushGeometryClip(geometry))
        {
            var center = ToScreen(WorldCenterX + spec.Center.X, WorldCenterY + spec.Center.Y);
            context.DrawEllipse(
                RadialGradient(
                    WithAlpha(accent, focusedRegion ? (byte)72 : (byte)42),
                    WithAlpha(accent, 0),
                    0.64),
                null,
                new Point(
                    center.X - spec.RadiusX * 0.24 * zoom,
                    center.Y - spec.RadiusY * 0.30 * zoom),
                spec.RadiusX * 0.78 * zoom,
                spec.RadiusY * 0.72 * zoom);

            context.DrawEllipse(
                RadialGradient(
                    WithAlpha(palette.Shadow, 52),
                    WithAlpha(palette.Shadow, 0),
                    0.68),
                null,
                new Point(
                    center.X + spec.RadiusX * 0.38 * zoom,
                    center.Y + spec.RadiusY * 0.42 * zoom),
                spec.RadiusX * 0.72 * zoom,
                spec.RadiusY * 0.62 * zoom);
        }

        DrawRegionContours(context, spec, accent, focusedRegion, angular);

        if (theme == AtlasThemePreference.Technical)
            DrawRegionDatum(context, spec, accent);
        else if (theme == AtlasThemePreference.Machine)
            DrawRegionCircuitMarks(context, spec, accent);
        else if (theme == AtlasThemePreference.World)
            DrawRegionTopography(context, spec, accent);
    }

    private void DrawRegionContours(
        DrawingContext context,
        RegionSpec spec,
        Color accent,
        bool focusedRegion,
        bool angular)
    {
        var count = theme switch
        {
            AtlasThemePreference.World => 3,
            AtlasThemePreference.Elegant => 2,
            AtlasThemePreference.Machine => 1,
            _ => 2
        };

        for (var index = 0; index < count; index++)
        {
            var scale = 0.80 - index * 0.16;
            var alpha = (byte)(focusedRegion ? 38 - index * 7 : 25 - index * 5);
            context.DrawGeometry(
                null,
                new Pen(new SolidColorBrush(WithAlpha(accent, alpha)), 0.9),
                CreateRegionGeometry(spec, scale, angular, default));
        }
    }

    private void DrawRegionDatum(DrawingContext context, RegionSpec spec, Color accent)
    {
        var center = ToScreen(WorldCenterX + spec.Center.X, WorldCenterY + spec.Center.Y);
        var pen = new Pen(new SolidColorBrush(WithAlpha(accent, 28)), 1);
        context.DrawLine(
            pen,
            new Point(center.X - spec.RadiusX * 0.54 * zoom, center.Y),
            new Point(center.X + spec.RadiusX * 0.54 * zoom, center.Y));
        DrawCrosshair(context, center, Math.Max(10d, 14d * zoom), new SolidColorBrush(WithAlpha(accent, 48)));
    }

    private void DrawRegionCircuitMarks(DrawingContext context, RegionSpec spec, Color accent)
    {
        var center = ToScreen(WorldCenterX + spec.Center.X, WorldCenterY + spec.Center.Y);
        var pen = new Pen(new SolidColorBrush(WithAlpha(accent, 34)), 1.1);
        var dx = spec.RadiusX * 0.46 * zoom;
        var dy = spec.RadiusY * 0.34 * zoom;
        context.DrawLine(pen, new Point(center.X - dx, center.Y - dy), new Point(center.X - dx * 0.34, center.Y - dy));
        context.DrawLine(pen, new Point(center.X + dx * 0.34, center.Y + dy), new Point(center.X + dx, center.Y + dy));
    }

    private void DrawRegionTopography(DrawingContext context, RegionSpec spec, Color accent)
    {
        var center = ToScreen(WorldCenterX + spec.Center.X, WorldCenterY + spec.Center.Y);
        var pen = new Pen(new SolidColorBrush(WithAlpha(accent, 20)), 0.9);
        context.DrawEllipse(
            null,
            pen,
            new Point(center.X - spec.RadiusX * 0.16 * zoom, center.Y + spec.RadiusY * 0.08 * zoom),
            spec.RadiusX * 0.22 * zoom,
            spec.RadiusY * 0.16 * zoom);
        context.DrawEllipse(
            null,
            pen,
            new Point(center.X + spec.RadiusX * 0.22 * zoom, center.Y - spec.RadiusY * 0.18 * zoom),
            spec.RadiusX * 0.15 * zoom,
            spec.RadiusY * 0.11 * zoom);
    }

    private void DrawConnections(
        DrawingContext context,
        ScenePalette palette,
        IReadOnlySet<string> focused)
    {
        foreach (var connection in connections.Where(item => item.Kind == AtlasConnectionKind.Composition))
            DrawCompositionRoute(context, connection, focused, palette);

        foreach (var connection in connections.Where(item => item.Kind == AtlasConnectionKind.CapabilityOwnership))
            DrawOwnershipRoute(context, connection, focused, palette);

        foreach (var connection in connections.Where(item => item.Kind == AtlasConnectionKind.CapabilityDependency))
            DrawDependencyRoute(context, connection, focused, palette);
    }

    private void DrawCompositionRoute(
        DrawingContext context,
        AtlasConnectionPresentationViewModel connection,
        IReadOnlySet<string> focused,
        ScenePalette palette)
    {
        var startCenter = ScreenPoint(connection.Source);
        var endCenter = ScreenPoint(connection.Target);
        var (start, end) = InsetRoute(startCenter, endCenter, 72 * zoom, 48 * zoom);
        var control = CompositionControlPoint(connection, start, end);
        var accent = AccentFor(connection.Target, palette);
        var focusedConnection = selectedNodeId is not null
            && focused.Contains(connection.Source.NodeId)
            && focused.Contains(connection.Target.NodeId);
        var dim = selectedNodeId is not null && !focusedConnection;

        DrawQuadraticRoute(
            context,
            start,
            control,
            end,
            new Pen(new SolidColorBrush(WithAlpha(accent, dim ? (byte)6 : (byte)18)), 12 * zoom),
            new Pen(new SolidColorBrush(WithAlpha(accent, dim ? (byte)20 : focusedConnection ? (byte)112 : (byte)54)), 2.2 * zoom),
            null);
    }

    private void DrawOwnershipRoute(
        DrawingContext context,
        AtlasConnectionPresentationViewModel connection,
        IReadOnlySet<string> focused,
        ScenePalette palette)
    {
        if (selectedNodeId is null)
            return;

        var focusedConnection = focused.Contains(connection.Source.NodeId)
            && focused.Contains(connection.Target.NodeId);
        if (!focusedConnection)
            return;

        var startCenter = ScreenPoint(connection.Source);
        var endCenter = ScreenPoint(connection.Target);
        var (start, end) = InsetRoute(startCenter, endCenter, 44 * zoom, 8 * zoom);
        var control = MidPoint(start, end);
        var accent = AccentFor(connection.Source, palette);

        DrawQuadraticRoute(
            context,
            start,
            control,
            end,
            new Pen(new SolidColorBrush(WithAlpha(accent, 18)), 5 * zoom),
            new Pen(new SolidColorBrush(WithAlpha(accent, 124)), 1.1),
            null);
    }

    private void DrawDependencyRoute(
        DrawingContext context,
        AtlasConnectionPresentationViewModel connection,
        IReadOnlySet<string> focused,
        ScenePalette palette)
    {
        var startCenter = ScreenPoint(connection.Source);
        var endCenter = ScreenPoint(connection.Target);
        var (start, end) = InsetRoute(startCenter, endCenter, 8 * zoom, 50 * zoom);
        var control = DependencyControlPoint(connection, start, end);
        var focusedConnection = selectedNodeId is not null
            && focused.Contains(connection.Source.NodeId)
            && focused.Contains(connection.Target.NodeId);
        var dim = selectedNodeId is not null && !focusedConnection;
        var alpha = dim ? (byte)40 : focusedConnection ? (byte)240 : (byte)156;
        var mainPen = new Pen(new SolidColorBrush(WithAlpha(palette.Dependency, alpha)), 1.8 * zoom);

        DrawQuadraticRoute(
            context,
            start,
            control,
            end,
            new Pen(new SolidColorBrush(WithAlpha(palette.Dependency, dim ? (byte)8 : (byte)30)), 8 * zoom),
            mainPen,
            () => DrawArrowHead(context, control, end, mainPen));

        if (focusedConnection && !reducedMotion)
            DrawRouteEnergy(context, start, control, end, palette.Dependency);
    }

    private void DrawQuadraticRoute(
        DrawingContext context,
        Point start,
        Point control,
        Point end,
        Pen glowPen,
        Pen linePen,
        Action? after)
    {
        var geometry = new StreamGeometry();
        using (var geometryContext = geometry.Open())
        {
            geometryContext.BeginFigure(start, isFilled: false);
            geometryContext.QuadraticBezierTo(control, end, isStroked: true);
            geometryContext.EndFigure(isClosed: false);
        }

        context.DrawGeometry(null, glowPen, geometry);
        context.DrawGeometry(null, linePen, geometry);
        after?.Invoke();
    }

    private void DrawRouteEnergy(DrawingContext context, Point start, Point control, Point end, Color color)
    {
        var phase = AmbientPhase();
        for (var index = 0; index < 3; index++)
        {
            var t = (phase + index / 3d) % 1d;
            var point = QuadraticPoint(start, control, end, t);
            context.DrawEllipse(
                RadialGradient(
                    WithAlpha(color, 210),
                    WithAlpha(color, 0),
                    0.64),
                null,
                point,
                7d,
                7d);
            context.DrawEllipse(new SolidColorBrush(WithAlpha(color, 230)), null, point, 1.6, 1.6);
        }
    }

    private void DrawProductNode(
        DrawingContext context,
        AtlasNodePresentationViewModel node,
        IReadOnlySet<string> focused,
        ScenePalette palette)
    {
        var center = ScreenPoint(node);
        var selected = string.Equals(node.NodeId, selectedNodeId, StringComparison.Ordinal);
        var contextual = selectedNodeId is null || selected || focused.Contains(node.NodeId);
        var opacity = contextual ? 1d : 0.30d;
        var accent = AccentFor(node, palette);

        using (context.PushOpacity(opacity))
        {
            var radius = (node.IsCore ? 58d : 43d) * zoom;
            var pulse = selected ? SelectedPulse() : 1d;

            context.DrawEllipse(
                RadialGradient(
                    WithAlpha(accent, selected ? (byte)96 : (byte)58),
                    WithAlpha(accent, 0),
                    0.64),
                null,
                center,
                (radius + 26 * zoom) * pulse,
                (radius + 26 * zoom) * pulse);

            if (selected)
            {
                context.DrawEllipse(
                    null,
                    new Pen(new SolidColorBrush(WithAlpha(accent, 126)), 1),
                    center,
                    radius + 14 * zoom,
                    radius + 14 * zoom);
            }

            DrawLandmarkBody(context, node, center, radius, accent, palette, selected);
            DrawNodeSigil(
                context,
                node,
                center,
                radius * 0.48,
                new Pen(new SolidColorBrush(WithAlpha(accent, 235)), node.IsCore ? 2.1 : 1.7));
            DrawNodeStatus(context, node, center, radius, palette);

            DrawCenteredText(
                context,
                node.IsCore ? "Wiiii Got This" : node.Title,
                new Point(center.X, center.Y + radius + 10 * zoom),
                Math.Clamp((node.IsCore ? 13d : 11.4d) * zoom, 9d, 15d),
                palette.Text);
            DrawCenteredText(
                context,
                node.CompactStateText,
                new Point(center.X, center.Y + radius + 26 * zoom),
                Math.Clamp(8d * zoom, 7d, 10d),
                WithAlpha(palette.Muted, 220));
        }
    }

    private void DrawLandmarkBody(
        DrawingContext context,
        AtlasNodePresentationViewModel node,
        Point center,
        double radius,
        Color accent,
        ScenePalette palette,
        bool selected)
    {
        var innerFill = RadialGradient(
            WithAlpha(Mix(palette.RegionSurface, accent, 0.28), selected ? (byte)248 : (byte)232),
            WithAlpha(palette.RegionDepth, 242),
            0.78);
        var outline = new Pen(new SolidColorBrush(WithAlpha(accent, selected ? (byte)245 : (byte)174)), selected ? 1.8 : 1.15);

        switch (theme)
        {
            case AtlasThemePreference.Technical:
                context.DrawEllipse(innerFill, outline, center, radius, radius);
                DrawCrosshair(context, center, radius + 8 * zoom, new SolidColorBrush(WithAlpha(accent, 96)));
                break;
            case AtlasThemePreference.Elegant:
                context.DrawEllipse(innerFill, null, center, radius + 3 * zoom, radius + 3 * zoom);
                context.DrawEllipse(null, outline, center, radius, radius);
                context.DrawEllipse(
                    null,
                    new Pen(new SolidColorBrush(WithAlpha(palette.Secondary, 72)), 0.8),
                    center,
                    radius - 7 * zoom,
                    radius - 7 * zoom);
                break;
            case AtlasThemePreference.Machine:
                var rect = new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2);
                context.FillRectangle(innerFill, rect);
                context.DrawRectangle(null, outline, rect);
                DrawMachineCorners(context, rect, 12 * zoom, new Pen(new SolidColorBrush(WithAlpha(accent, 232)), selected ? 2.2 : 1.5));
                break;
            case AtlasThemePreference.World:
                context.DrawEllipse(innerFill, outline, center, radius, radius * 0.91);
                context.DrawEllipse(
                    new SolidColorBrush(WithAlpha(accent, 15)),
                    null,
                    new Point(center.X - radius * 0.18, center.Y - radius * 0.14),
                    radius * 0.62,
                    radius * 0.48);
                break;
        }

        if (node.IsCore)
        {
            context.DrawEllipse(
                null,
                new Pen(new SolidColorBrush(WithAlpha(palette.Core, 70)), 1),
                center,
                radius + 20 * zoom,
                radius + 14 * zoom);
        }
    }

    private void DrawCapability(
        DrawingContext context,
        AtlasNodePresentationViewModel node,
        IReadOnlySet<string> focused,
        ScenePalette palette)
    {
        var center = ScreenPoint(node);
        var selected = string.Equals(node.NodeId, selectedNodeId, StringComparison.Ordinal);
        var expanded = selectedNodeId is not null && focused.Contains(node.NodeId);
        var contextual = selectedNodeId is null || expanded;
        var radius = Math.Max(4d, 5.5d * zoom);
        var accent = AccentFor(node, palette);
        var color = node.IsAvailable ? accent : palette.Unavailable;

        using (context.PushOpacity(contextual ? 0.96 : 0.16))
        {
            context.DrawEllipse(
                RadialGradient(
                    WithAlpha(color, selected ? (byte)224 : (byte)136),
                    WithAlpha(color, 0),
                    0.68),
                null,
                center,
                radius + 8 * zoom,
                radius + 8 * zoom);
            context.DrawEllipse(
                new SolidColorBrush(WithAlpha(color, selected ? (byte)255 : (byte)220)),
                selected ? new Pen(new SolidColorBrush(palette.Text), 1) : null,
                center,
                selected ? radius + 2 : radius,
                selected ? radius + 2 : radius);

            if (expanded)
            {
                var extendRight = node.X >= 0;
                var labelPoint = new Point(
                    center.X + (extendRight ? 12 : -12) * zoom,
                    center.Y - 8 * zoom);
                DrawAnchoredText(
                    context,
                    node.Title,
                    labelPoint,
                    extendRight,
                    Math.Clamp(9.4d * zoom, 8d, 11d),
                    palette.Text);
            }
        }
    }

    private void DrawNodeSigil(
        DrawingContext context,
        AtlasNodePresentationViewModel node,
        Point center,
        double radius,
        Pen pen)
    {
        if (node.IsCore)
        {
            context.DrawEllipse(null, pen, center, radius * 0.58, radius * 0.58);
            context.DrawLine(pen, new Point(center.X - radius, center.Y), new Point(center.X - radius * 0.40, center.Y));
            context.DrawLine(pen, new Point(center.X + radius * 0.40, center.Y), new Point(center.X + radius, center.Y));
            context.DrawLine(pen, new Point(center.X, center.Y - radius), new Point(center.X, center.Y - radius * 0.40));
            context.DrawLine(pen, new Point(center.X, center.Y + radius * 0.40), new Point(center.X, center.Y + radius));
            return;
        }

        switch (node.ServiceIdentity?.Value)
        {
            case "vocation":
                context.DrawLine(pen, new Point(center.X - radius * 0.70, center.Y - radius * 0.55), new Point(center.X, center.Y + radius * 0.65));
                context.DrawLine(pen, new Point(center.X, center.Y + radius * 0.65), new Point(center.X + radius * 0.70, center.Y - radius * 0.55));
                break;
            case "illumination":
                context.DrawEllipse(null, pen, center, radius * 0.33, radius * 0.33);
                for (var index = 0; index < 8; index++)
                {
                    var angle = Math.PI * 2 * index / 8d;
                    var inner = new Point(center.X + Math.Cos(angle) * radius * 0.52, center.Y + Math.Sin(angle) * radius * 0.52);
                    var outer = new Point(center.X + Math.Cos(angle) * radius * 0.88, center.Y + Math.Sin(angle) * radius * 0.88);
                    context.DrawLine(pen, inner, outer);
                }
                break;
            case "orientation":
                var top = new Point(center.X, center.Y - radius * 0.88);
                var right = new Point(center.X + radius * 0.55, center.Y);
                var bottom = new Point(center.X, center.Y + radius * 0.88);
                var left = new Point(center.X - radius * 0.55, center.Y);
                context.DrawLine(pen, top, right);
                context.DrawLine(pen, right, bottom);
                context.DrawLine(pen, bottom, left);
                context.DrawLine(pen, left, top);
                context.DrawLine(pen, center, top);
                break;
            case "conveyance":
                context.DrawLine(pen, new Point(center.X - radius * 0.72, center.Y - radius * 0.34), new Point(center.X + radius * 0.72, center.Y - radius * 0.34));
                context.DrawLine(pen, new Point(center.X - radius * 0.72, center.Y + radius * 0.34), new Point(center.X + radius * 0.72, center.Y + radius * 0.34));
                context.DrawLine(pen, new Point(center.X + radius * 0.30, center.Y - radius * 0.64), new Point(center.X + radius * 0.72, center.Y - radius * 0.34));
                context.DrawLine(pen, new Point(center.X + radius * 0.30, center.Y + radius * 0.04), new Point(center.X + radius * 0.72, center.Y + radius * 0.34));
                break;
            default:
                context.DrawEllipse(null, pen, center, radius * 0.62, radius * 0.62);
                context.DrawLine(pen, new Point(center.X - radius * 0.72, center.Y), new Point(center.X + radius * 0.72, center.Y));
                break;
        }
    }

    private void DrawNodeStatus(
        DrawingContext context,
        AtlasNodePresentationViewModel node,
        Point center,
        double radius,
        ScenePalette palette)
    {
        if (node.IsCore)
            return;

        var statusColor = node.IsAvailable ? palette.Available : palette.Unavailable;
        var statusCenter = new Point(center.X + radius * 0.72, center.Y - radius * 0.72);
        context.DrawEllipse(
            RadialGradient(
                WithAlpha(statusColor, 210),
                WithAlpha(statusColor, 0),
                0.65),
            null,
            statusCenter,
            7,
            7);
        context.DrawEllipse(new SolidColorBrush(statusColor), null, statusCenter, 2.8, 2.8);
    }

    private void DrawVignette(DrawingContext context, ScenePalette palette)
    {
        var brush = new RadialGradientBrush
        {
            Center = new RelativePoint(0.50, 0.47, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0.48, 0.43, RelativeUnit.Relative)
        };
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.00));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.62));
        brush.GradientStops.Add(new GradientStop(WithAlpha(palette.Shadow, 34), 0.84));
        brush.GradientStops.Add(new GradientStop(WithAlpha(palette.Shadow, 82), 1.00));
        context.FillRectangle(brush, new Rect(Bounds.Size));
    }

    private AtlasNodePresentationViewModel? HitTestNode(Point screenPoint)
    {
        var focused = AtlasPresentationFocus.Build(connections, selectedNodeId);
        foreach (var node in nodes.OrderByDescending(node => node.IsCapability ? 1 : 2))
        {
            var center = ScreenPoint(node);
            var radius = node.Kind switch
            {
                AtlasNodeKind.Core => Math.Max(42d, 68d * zoom),
                AtlasNodeKind.Service => Math.Max(34d, 52d * zoom),
                AtlasNodeKind.Capability when selectedNodeId is not null && focused.Contains(node.NodeId) => Math.Max(16d, 22d * zoom),
                _ => Math.Max(11d, 15d * zoom)
            };
            var delta = screenPoint - center;
            if (delta.X * delta.X + delta.Y * delta.Y <= radius * radius)
                return node;
        }

        return null;
    }

    private Point ScreenPoint(AtlasNodePresentationViewModel node) =>
        ToScreen(WorldCenterX + node.X, WorldCenterY + node.Y);

    private Point ToScreen(double worldX, double worldY) =>
        new(worldX * zoom + translateX, worldY * zoom + translateY);

    private Point CompositionControlPoint(
        AtlasConnectionPresentationViewModel connection,
        Point start,
        Point end)
    {
        var mid = MidPoint(start, end);
        var targetId = connection.Target.ServiceIdentity?.Value;
        var bend = targetId switch
        {
            "illumination" => new Vector(-36 * zoom, -8 * zoom),
            "orientation" => new Vector(18 * zoom, -26 * zoom),
            "conveyance" => new Vector(38 * zoom, 4 * zoom),
            "vocation" => new Vector(-20 * zoom, 28 * zoom),
            _ => default
        };
        return mid + bend;
    }

    private Point DependencyControlPoint(
        AtlasConnectionPresentationViewModel connection,
        Point start,
        Point end)
    {
        if (string.Equals(connection.Source.ServiceIdentity?.Value, "vocation", StringComparison.Ordinal)
            && string.Equals(connection.Target.ServiceIdentity?.Value, "orientation", StringComparison.Ordinal))
        {
            return ToScreen(WorldCenterX + 20, WorldCenterY + 205);
        }

        var span = end - start;
        var length = Math.Sqrt(span.X * span.X + span.Y * span.Y);
        if (length < 0.01)
            return MidPoint(start, end);

        var normal = new Vector(-span.Y / length, span.X / length);
        return MidPoint(start, end) + normal * 70 * zoom;
    }

    private static (Point Start, Point End) InsetRoute(
        Point startCenter,
        Point endCenter,
        double startInset,
        double endInset)
    {
        var delta = endCenter - startCenter;
        var length = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
        if (length < 0.01)
            return (startCenter, endCenter);

        var unit = new Vector(delta.X / length, delta.Y / length);
        return (startCenter + unit * startInset, endCenter - unit * endInset);
    }

    private RegionSpec CoreRegion() =>
        new(new Point(0, 0), 220, 150, CoreContour, "core");

    private RegionSpec RegionFor(AtlasNodePresentationViewModel service) => service.ServiceIdentity?.Value switch
    {
        "vocation" => new(new Point(-365, 0), 300, 198, VocationContour, "vocation"),
        "illumination" => new(new Point(0, -255), 258, 190, IlluminationContour, "illumination"),
        "orientation" => new(new Point(365, 0), 292, 200, OrientationContour, "orientation"),
        "conveyance" => new(new Point(0, 255), 310, 186, ConveyanceContour, "conveyance"),
        _ => new(new Point(service.X, service.Y), 190, 145, CoreContour, service.ServiceIdentity?.Value ?? "service")
    };

    private StreamGeometry CreateRegionGeometry(
        RegionSpec spec,
        double scale,
        bool angular,
        Vector pixelOffset)
    {
        var points = spec.Contour
            .Select(point => ToScreen(
                WorldCenterX + spec.Center.X + point.X * spec.RadiusX * scale,
                WorldCenterY + spec.Center.Y + point.Y * spec.RadiusY * scale) + pixelOffset)
            .ToArray();

        var geometry = new StreamGeometry();
        using var geometryContext = geometry.Open();

        if (angular)
        {
            geometryContext.BeginFigure(points[0], isFilled: true);
            for (var index = 1; index < points.Length; index++)
                geometryContext.LineTo(points[index], isStroked: true);
            geometryContext.EndFigure(isClosed: true);
            return geometry;
        }

        geometryContext.BeginFigure(MidPoint(points[^1], points[0]), isFilled: true);
        for (var index = 0; index < points.Length; index++)
        {
            var current = points[index];
            var next = points[(index + 1) % points.Length];
            geometryContext.QuadraticBezierTo(current, MidPoint(current, next), isStroked: true);
        }
        geometryContext.EndFigure(isClosed: true);
        return geometry;
    }

    private bool RegionIsFocused(
        AtlasNodePresentationViewModel service,
        IReadOnlySet<string> focused)
    {
        if (selectedNodeId is null)
            return false;
        if (focused.Contains(service.NodeId))
            return true;

        return nodes.Any(node =>
            node.IsCapability
            && node.ServiceIdentity == service.ServiceIdentity
            && focused.Contains(node.NodeId));
    }

    private Color AccentFor(AtlasNodePresentationViewModel node, ScenePalette palette)
    {
        if (node.IsCore)
            return palette.Core;

        return node.ServiceIdentity?.Value switch
        {
            "vocation" => palette.Vocation,
            "illumination" => palette.Illumination,
            "orientation" => palette.Orientation,
            "conveyance" => palette.Conveyance,
            _ => palette.Primary
        };
    }

    private static void DrawCenteredText(
        DrawingContext context,
        string text,
        Point topCenter,
        double fontSize,
        Color color)
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

    private static void DrawAnchoredText(
        DrawingContext context,
        string text,
        Point anchor,
        bool extendRight,
        double fontSize,
        Color color)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            fontSize,
            new SolidColorBrush(color));
        var x = extendRight ? anchor.X : anchor.X - formatted.Width;
        context.DrawText(formatted, new Point(x, anchor.Y));
    }

    private static void DrawCrosshair(DrawingContext context, Point center, double radius, IBrush brush)
    {
        var pen = new Pen(brush, 1);
        var tick = Math.Min(7d, radius * 0.42);
        context.DrawLine(pen, new Point(center.X - radius, center.Y), new Point(center.X - radius + tick, center.Y));
        context.DrawLine(pen, new Point(center.X + radius - tick, center.Y), new Point(center.X + radius, center.Y));
        context.DrawLine(pen, new Point(center.X, center.Y - radius), new Point(center.X, center.Y - radius + tick));
        context.DrawLine(pen, new Point(center.X, center.Y + radius - tick), new Point(center.X, center.Y + radius));
    }

    private static void DrawMachineCorners(DrawingContext context, Rect rect, double arm, Pen pen)
    {
        context.DrawLine(pen, rect.TopLeft, new Point(rect.Left + arm, rect.Top));
        context.DrawLine(pen, rect.TopLeft, new Point(rect.Left, rect.Top + arm));
        context.DrawLine(pen, rect.TopRight, new Point(rect.Right - arm, rect.Top));
        context.DrawLine(pen, rect.TopRight, new Point(rect.Right, rect.Top + arm));
        context.DrawLine(pen, rect.BottomLeft, new Point(rect.Left + arm, rect.Bottom));
        context.DrawLine(pen, rect.BottomLeft, new Point(rect.Left, rect.Bottom - arm));
        context.DrawLine(pen, rect.BottomRight, new Point(rect.Right - arm, rect.Bottom));
        context.DrawLine(pen, rect.BottomRight, new Point(rect.Right, rect.Bottom - arm));
    }

    private static void DrawArrowHead(DrawingContext context, Point control, Point end, Pen pen)
    {
        var tangent = end - control;
        var length = Math.Sqrt(tangent.X * tangent.X + tangent.Y * tangent.Y);
        if (length < 0.01)
            return;

        var unit = new Vector(tangent.X / length, tangent.Y / length);
        var perpendicular = new Vector(-unit.Y, unit.X);
        var basePoint = end - unit * 9d;
        context.DrawLine(pen, end, basePoint + perpendicular * 4d);
        context.DrawLine(pen, end, basePoint - perpendicular * 4d);
    }

    private double ThemeTransitionProgress()
    {
        if (!themeTransitionActive || reducedMotion)
            return 1d;

        var elapsed = DateTime.UtcNow - themeTransitionStartedUtc;
        if (elapsed >= ThemeTransitionDuration)
        {
            themeTransitionActive = false;
            previousTheme = theme;
            return 1d;
        }

        var t = Math.Clamp(
            elapsed.TotalMilliseconds / ThemeTransitionDuration.TotalMilliseconds,
            0d,
            1d);
        return 1d - Math.Pow(1d - t, 3d);
    }

    private double SelectedPulse()
    {
        if (reducedMotion)
            return 1d;
        return 1d + Math.Sin(DateTime.UtcNow.TimeOfDay.TotalSeconds * Math.PI * 1.15) * 0.035;
    }

    private double AmbientPhase()
    {
        if (reducedMotion)
            return 0.45d;
        return DateTime.UtcNow.TimeOfDay.TotalSeconds * 0.10 % 1d;
    }

    private void RequestSceneFrame()
    {
        var animateSelection = !reducedMotion && selectedNodeId is not null;
        if (!themeTransitionActive && !animateSelection)
            return;

        TopLevel.GetTopLevel(this)?.RequestAnimationFrame(_ => InvalidateVisual());
    }

    private static Point QuadraticPoint(Point start, Point control, Point end, double t)
    {
        var oneMinus = 1d - t;
        return new Point(
            oneMinus * oneMinus * start.X + 2d * oneMinus * t * control.X + t * t * end.X,
            oneMinus * oneMinus * start.Y + 2d * oneMinus * t * control.Y + t * t * end.Y);
    }

    private static Point MidPoint(Point first, Point second) =>
        new((first.X + second.X) / 2d, (first.Y + second.Y) / 2d);

    private static double Mod(double value, double modulus)
    {
        if (modulus <= 0)
            return 0;
        var result = value % modulus;
        return result < 0 ? result + modulus : result;
    }

    private static LinearGradientBrush LinearGradient(
        Color start,
        Color end,
        RelativePoint startPoint,
        RelativePoint endPoint)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = startPoint,
            EndPoint = endPoint
        };
        brush.GradientStops.Add(new GradientStop(start, 0));
        brush.GradientStops.Add(new GradientStop(end, 1));
        return brush;
    }

    private static RadialGradientBrush RadialGradient(
        Color center,
        Color edge,
        double edgeOffset)
    {
        var brush = new RadialGradientBrush
        {
            Center = RelativePoint.Center,
            GradientOrigin = new RelativePoint(0.42, 0.36, RelativeUnit.Relative)
        };
        brush.GradientStops.Add(new GradientStop(center, 0));
        brush.GradientStops.Add(new GradientStop(Mix(center, edge, 0.62), Math.Clamp(edgeOffset * 0.58, 0.15, 0.78)));
        brush.GradientStops.Add(new GradientStop(edge, Math.Clamp(edgeOffset, 0.55, 1.0)));
        brush.GradientStops.Add(new GradientStop(edge, 1));
        return brush;
    }

    private static Color Mix(Color from, Color to, double t) => Color.FromArgb(
        LerpByte(from.A, to.A, t),
        LerpByte(from.R, to.R, t),
        LerpByte(from.G, to.G, t),
        LerpByte(from.B, to.B, t));

    private static byte LerpByte(byte from, byte to, double t) =>
        (byte)Math.Clamp(Math.Round(from + (to - from) * t), byte.MinValue, byte.MaxValue);

    private static Color WithAlpha(Color color, byte alpha) =>
        Color.FromArgb(alpha, color.R, color.G, color.B);

    private readonly record struct RegionSpec(
        Point Center,
        double RadiusX,
        double RadiusY,
        IReadOnlyList<Point> Contour,
        string Identity);

    private readonly record struct ScenePalette(
        Color BackgroundTop,
        Color BackgroundBottom,
        Color RegionSurface,
        Color RegionDepth,
        Color Shadow,
        Color Primary,
        Color Secondary,
        Color Edge,
        Color Dependency,
        Color Text,
        Color Muted,
        Color Available,
        Color Unavailable,
        Color Core,
        Color Vocation,
        Color Illumination,
        Color Orientation,
        Color Conveyance)
    {
        public static ScenePalette For(AtlasThemePreference theme) => theme switch
        {
            AtlasThemePreference.Elegant => new(
                Color.Parse("#171315"), Color.Parse("#0F0D10"),
                Color.Parse("#282124"), Color.Parse("#121012"), Color.Parse("#030203"),
                Color.Parse("#D7BDA5"), Color.Parse("#755E69"), Color.Parse("#7A6570"),
                Color.Parse("#E1B1A6"), Color.Parse("#F3E8DF"), Color.Parse("#A89590"),
                Color.Parse("#9CD4B4"), Color.Parse("#B87878"),
                Color.Parse("#E3CCB8"), Color.Parse("#B5D6C4"), Color.Parse("#E4C59A"),
                Color.Parse("#A9C4D6"), Color.Parse("#C3B3D8")),
            AtlasThemePreference.Machine => new(
                Color.Parse("#06110B"), Color.Parse("#020805"),
                Color.Parse("#0A2116"), Color.Parse("#031009"), Color.Parse("#000302"),
                Color.Parse("#72F1AE"), Color.Parse("#2A7651"), Color.Parse("#2F8B62"),
                Color.Parse("#C2F285"), Color.Parse("#DFFFF0"), Color.Parse("#76A88B"),
                Color.Parse("#73EFAE"), Color.Parse("#D57272"),
                Color.Parse("#72F1AE"), Color.Parse("#79E3B2"), Color.Parse("#C4E987"),
                Color.Parse("#7BC9C1"), Color.Parse("#9CAE7B")),
            AtlasThemePreference.World => new(
                Color.Parse("#08140F"), Color.Parse("#030906"),
                Color.Parse("#10271D"), Color.Parse("#07150F"), Color.Parse("#000302"),
                Color.Parse("#89D8AD"), Color.Parse("#35624A"), Color.Parse("#47775A"),
                Color.Parse("#D7D887"), Color.Parse("#E8F5EC"), Color.Parse("#84A58F"),
                Color.Parse("#92DFB5"), Color.Parse("#CF7676"),
                Color.Parse("#8DE8C2"), Color.Parse("#7ED6B2"), Color.Parse("#E1C46A"),
                Color.Parse("#75BEEA"), Color.Parse("#A395E6")),
            _ => new(
                Color.Parse("#07131A"), Color.Parse("#02080C"),
                Color.Parse("#0C202A"), Color.Parse("#061117"), Color.Parse("#000307"),
                Color.Parse("#68DDF5"), Color.Parse("#255E72"), Color.Parse("#337E96"),
                Color.Parse("#A7CAFF"), Color.Parse("#E3FAFF"), Color.Parse("#789CA8"),
                Color.Parse("#6DE0C1"), Color.Parse("#D76F7B"),
                Color.Parse("#70E6EE"), Color.Parse("#62DDBB"), Color.Parse("#D6C879"),
                Color.Parse("#79B8EF"), Color.Parse("#A28DE5"))
        };

        public static ScenePalette Lerp(ScenePalette from, ScenePalette to, double t) => new(
            Mix(from.BackgroundTop, to.BackgroundTop, t),
            Mix(from.BackgroundBottom, to.BackgroundBottom, t),
            Mix(from.RegionSurface, to.RegionSurface, t),
            Mix(from.RegionDepth, to.RegionDepth, t),
            Mix(from.Shadow, to.Shadow, t),
            Mix(from.Primary, to.Primary, t),
            Mix(from.Secondary, to.Secondary, t),
            Mix(from.Edge, to.Edge, t),
            Mix(from.Dependency, to.Dependency, t),
            Mix(from.Text, to.Text, t),
            Mix(from.Muted, to.Muted, t),
            Mix(from.Available, to.Available, t),
            Mix(from.Unavailable, to.Unavailable, t),
            Mix(from.Core, to.Core, t),
            Mix(from.Vocation, to.Vocation, t),
            Mix(from.Illumination, to.Illumination, t),
            Mix(from.Orientation, to.Orientation, t),
            Mix(from.Conveyance, to.Conveyance, t));
    }
}
