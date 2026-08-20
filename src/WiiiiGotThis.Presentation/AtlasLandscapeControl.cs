using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

/// <summary>
/// Production Atlas landscape renderer. It renders the downstream AtlasLandscape topology;
/// WGT semantics continue to come from the projection/presentation model and provider Product
/// Surfaces remain normal hosted product UI outside this control.
/// </summary>
public sealed class AtlasLandscapeControl : Control
{
    private const double WorldCenterX = 1000d;
    private const double WorldCenterY = 660d;
    private static readonly TimeSpan ThemeTransitionDuration = TimeSpan.FromMilliseconds(240);

    private IReadOnlyList<AtlasNodePresentationViewModel> nodes = Array.Empty<AtlasNodePresentationViewModel>();
    private IReadOnlyList<AtlasConnectionPresentationViewModel> connections = Array.Empty<AtlasConnectionPresentationViewModel>();
    private AtlasLandscape? landscape;
    private string? selectedNodeId;
    private AtlasThemePreference theme = AtlasThemePreference.Technical;
    private AtlasThemePreference previousTheme = AtlasThemePreference.Technical;
    private DateTime themeTransitionStartedUtc;
    private bool themeTransitionActive;
    private bool reducedMotion;
    private double zoom = 0.82d;
    private double translateX;
    private double translateY;

    public AtlasLandscapeControl()
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
        landscape = AtlasLandscapeBuilder.Build(nextNodes, nextConnections);
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
            previousTheme = theme;
            themeTransitionActive = false;
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
        if (landscape is null)
            return;

        var transition = ThemeTransitionProgress();
        var palette = ScenePalette.Lerp(
            ScenePalette.For(previousTheme),
            ScenePalette.For(theme),
            transition);
        var focused = AtlasPresentationFocus.Build(connections, selectedNodeId);

        DrawBackdrop(context, palette);
        DrawWorldFrame(context, palette);
        DrawAtmosphere(context, palette, focused);
        DrawRegions(context, palette, focused);
        DrawRoutes(context, palette, focused);
        DrawGates(context, palette, focused);
        DrawCapabilityLandmarks(context, palette, focused);
        DrawServiceLandmarks(context, palette, focused);
        DrawCoreNexus(context, palette, focused);
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
        context.FillRectangle(
            LinearGradient(
                palette.BackgroundTop,
                palette.BackgroundBottom,
                new RelativePoint(0.08, 0.04, RelativeUnit.Relative),
                new RelativePoint(0.92, 0.96, RelativeUnit.Relative)),
            new Rect(Bounds.Size));

        var nexus = ToScreen(WorldCenterX, WorldCenterY);
        context.DrawEllipse(
            RadialGradient(WithAlpha(palette.Core, 32), WithAlpha(palette.Core, 0), 0.72),
            null,
            nexus,
            390 * zoom,
            310 * zoom);
    }

    private void DrawWorldFrame(DrawingContext context, ScenePalette palette)
    {
        switch (theme)
        {
            case AtlasThemePreference.Technical:
                DrawTechnicalGrid(context, palette);
                break;
            case AtlasThemePreference.Elegant:
                DrawElegantField(context, palette);
                break;
            case AtlasThemePreference.Machine:
                DrawMachineFrame(context, palette);
                break;
            case AtlasThemePreference.World:
                DrawWorldContours(context, palette);
                break;
        }
    }

    private void DrawTechnicalGrid(DrawingContext context, ScenePalette palette)
    {
        var minor = new Pen(new SolidColorBrush(WithAlpha(palette.Primary, 11)), 1);
        var major = new Pen(new SolidColorBrush(WithAlpha(palette.Primary, 20)), 1);
        const double worldSpacing = 82;
        var step = Math.Max(23, worldSpacing * zoom);
        var xOffset = Mod(translateX, step);
        var yOffset = Mod(translateY, step);

        for (var index = -1; xOffset + index * step <= Bounds.Width + step; index++)
        {
            var x = xOffset + index * step;
            context.DrawLine(index % 4 == 0 ? major : minor, new Point(x, 0), new Point(x, Bounds.Height));
        }

        for (var index = -1; yOffset + index * step <= Bounds.Height + step; index++)
        {
            var y = yOffset + index * step;
            context.DrawLine(index % 4 == 0 ? major : minor, new Point(0, y), new Point(Bounds.Width, y));
        }
    }

    private void DrawElegantField(DrawingContext context, ScenePalette palette)
    {
        var center = ToScreen(WorldCenterX, WorldCenterY);
        context.DrawEllipse(null, new Pen(new SolidColorBrush(WithAlpha(palette.Secondary, 14)), 1), center, 610 * zoom, 420 * zoom);
        context.DrawEllipse(null, new Pen(new SolidColorBrush(WithAlpha(palette.Primary, 9)), 1), center, 760 * zoom, 520 * zoom);
    }

    private void DrawMachineFrame(DrawingContext context, ScenePalette palette)
    {
        var center = ToScreen(WorldCenterX, WorldCenterY);
        var frame = new Rect(center.X - 680 * zoom, center.Y - 470 * zoom, 1360 * zoom, 940 * zoom);
        var pen = new Pen(new SolidColorBrush(WithAlpha(palette.Primary, 18)), 1);
        context.DrawRectangle(null, pen, frame);
        DrawMachineCorners(context, frame, 42 * zoom, new Pen(new SolidColorBrush(WithAlpha(palette.Primary, 50)), 1.3));
    }

    private void DrawWorldContours(DrawingContext context, ScenePalette palette)
    {
        var center = ToScreen(WorldCenterX, WorldCenterY);
        var pen = new Pen(new SolidColorBrush(WithAlpha(palette.Secondary, 12)), 1);
        context.DrawEllipse(null, pen, new Point(center.X - 50 * zoom, center.Y - 25 * zoom), 680 * zoom, 390 * zoom);
        context.DrawEllipse(null, pen, new Point(center.X + 85 * zoom, center.Y + 70 * zoom), 590 * zoom, 335 * zoom);
        context.DrawEllipse(null, new Pen(new SolidColorBrush(WithAlpha(palette.Primary, 8)), 1), center, 805 * zoom, 515 * zoom);
    }

    private void DrawAtmosphere(
        DrawingContext context,
        ScenePalette palette,
        IReadOnlySet<string> focused)
    {
        if (landscape is null)
            return;

        foreach (var region in landscape.Regions)
        {
            var service = FindNode(region.NodeId);
            if (service is null)
                continue;

            var accent = AccentFor(service, palette);
            var focus = RegionFocused(region, focused);
            var center = ToScreen(WorldCenterX + region.Landmark.X, WorldCenterY + region.Landmark.Y);
            var alpha = focus ? (byte)48 : service.IsAvailable ? (byte)28 : (byte)16;

            context.DrawEllipse(
                RadialGradient(WithAlpha(accent, alpha), WithAlpha(accent, 0), 0.72),
                null,
                center,
                340 * zoom,
                250 * zoom);
        }
    }

    private void DrawRegions(
        DrawingContext context,
        ScenePalette palette,
        IReadOnlySet<string> focused)
    {
        if (landscape is null)
            return;

        foreach (var region in landscape.Regions)
        {
            var service = FindNode(region.NodeId);
            if (service is null)
                continue;

            DrawRegion(context, region, service, palette, focused);
        }
    }

    private void DrawRegion(
        DrawingContext context,
        AtlasLandscapeRegion region,
        AtlasNodePresentationViewModel service,
        ScenePalette palette,
        IReadOnlySet<string> focused)
    {
        var accent = AccentFor(service, palette);
        var regionFocused = RegionFocused(region, focused);
        var angular = theme == AtlasThemePreference.Machine;
        var geometry = CreateRegionGeometry(region, 1d, angular, default);
        var shadow = CreateRegionGeometry(region, 1d, angular, new Vector(0, Math.Max(4, 10 * zoom)));

        context.DrawGeometry(
            new SolidColorBrush(WithAlpha(palette.Shadow, service.IsAvailable ? (byte)124 : (byte)94)),
            null,
            shadow);

        var surface = Mix(palette.RegionSurface, accent, service.IsAvailable ? 0.14 : 0.06);
        var depth = Mix(palette.RegionDepth, accent, service.IsAvailable ? 0.06 : 0.02);
        context.DrawGeometry(
            LinearGradient(
                WithAlpha(surface, regionFocused ? (byte)246 : (byte)229),
                WithAlpha(depth, regionFocused ? (byte)242 : (byte)218),
                new RelativePoint(0.16, 0.06, RelativeUnit.Relative),
                new RelativePoint(0.84, 0.96, RelativeUnit.Relative)),
            new Pen(new SolidColorBrush(WithAlpha(accent, regionFocused ? (byte)128 : (byte)68)), regionFocused ? 1.45 : 1.0),
            geometry);

        using (context.PushGeometryClip(geometry))
        {
            var landmark = ScreenPoint(region.Landmark);
            context.DrawEllipse(
                RadialGradient(
                    WithAlpha(accent, regionFocused ? (byte)66 : (byte)38),
                    WithAlpha(accent, 0),
                    0.68),
                null,
                new Point(landmark.X - 55 * zoom, landmark.Y - 54 * zoom),
                235 * zoom,
                190 * zoom);

            var gate = ScreenPoint(region.InnerGate);
            context.DrawEllipse(
                RadialGradient(WithAlpha(palette.Core, 28), WithAlpha(palette.Core, 0), 0.72),
                null,
                gate,
                118 * zoom,
                88 * zoom);
        }

        var contourCount = theme switch
        {
            AtlasThemePreference.World => 3,
            AtlasThemePreference.Elegant => 1,
            AtlasThemePreference.Machine => 1,
            _ => 2
        };

        for (var index = 0; index < contourCount; index++)
        {
            var scale = 0.84 - index * 0.14;
            context.DrawGeometry(
                null,
                new Pen(new SolidColorBrush(WithAlpha(accent, (byte)(regionFocused ? 30 - index * 6 : 18 - index * 4))), 0.9),
                CreateRegionGeometry(region, scale, angular, default));
        }

        DrawRegionLanguage(context, region, service, accent, palette);
        DrawRegionLabel(context, region, service, palette, accent);
    }

    private void DrawRegionLanguage(
        DrawingContext context,
        AtlasLandscapeRegion region,
        AtlasNodePresentationViewModel service,
        Color accent,
        ScenePalette palette)
    {
        var landmark = ScreenPoint(region.Landmark);
        switch (theme)
        {
            case AtlasThemePreference.Technical:
                var technicalPen = new Pen(new SolidColorBrush(WithAlpha(accent, 26)), 1);
                var gate = ScreenPoint(region.InnerGate);
                context.DrawLine(technicalPen, new Point(gate.X - 16 * zoom, gate.Y), new Point(gate.X + 16 * zoom, gate.Y));
                DrawCrosshair(context, landmark, 18 * zoom, new SolidColorBrush(WithAlpha(accent, 42)));
                break;

            case AtlasThemePreference.Elegant:
                context.DrawEllipse(
                    null,
                    new Pen(new SolidColorBrush(WithAlpha(palette.Secondary, 28)), 0.9),
                    landmark,
                    96 * zoom,
                    64 * zoom);
                break;

            case AtlasThemePreference.Machine:
                var rect = new Rect(landmark.X - 122 * zoom, landmark.Y - 78 * zoom, 244 * zoom, 156 * zoom);
                DrawMachineCorners(context, rect, 20 * zoom, new Pen(new SolidColorBrush(WithAlpha(accent, 46)), 1));
                break;

            case AtlasThemePreference.World:
                context.DrawEllipse(
                    null,
                    new Pen(new SolidColorBrush(WithAlpha(accent, 22)), 0.9),
                    new Point(landmark.X - 42 * zoom, landmark.Y + 24 * zoom),
                    78 * zoom,
                    44 * zoom);
                context.DrawEllipse(
                    null,
                    new Pen(new SolidColorBrush(WithAlpha(accent, 16)), 0.9),
                    new Point(landmark.X + 55 * zoom, landmark.Y - 38 * zoom),
                    58 * zoom,
                    34 * zoom);
                break;
        }

        if (!service.IsAvailable)
        {
            context.DrawEllipse(
                null,
                new Pen(new SolidColorBrush(WithAlpha(palette.Unavailable, 52)), 1),
                landmark,
                86 * zoom,
                62 * zoom);
        }
    }

    private void DrawRegionLabel(
        DrawingContext context,
        AtlasLandscapeRegion region,
        AtlasNodePresentationViewModel service,
        ScenePalette palette,
        Color accent)
    {
        var anchor = ScreenPoint(region.LabelAnchor);
        var title = $"{service.Title.ToUpperInvariant()} DISTRICT";
        DrawAnchoredText(context, title, anchor, true, Math.Clamp(9.2 * zoom, 8.2, 11.5), WithAlpha(palette.Text, 205));
        DrawAnchoredText(
            context,
            region.CapabilityNodeIds.Count == 1 ? "1 CAPABILITY" : $"{region.CapabilityNodeIds.Count} CAPABILITIES",
            new Point(anchor.X, anchor.Y + 14 * zoom),
            true,
            Math.Clamp(7.4 * zoom, 6.8, 9.2),
            WithAlpha(accent, 150));
    }

    private void DrawRoutes(
        DrawingContext context,
        ScenePalette palette,
        IReadOnlySet<string> focused)
    {
        if (landscape is null)
            return;

        foreach (var route in landscape.Routes.Where(route => route.Kind == AtlasLandscapeRouteKind.CompositionCorridor))
            DrawCompositionCorridor(context, route, palette, focused);

        foreach (var route in landscape.Routes.Where(route => route.Kind == AtlasLandscapeRouteKind.DistrictPath))
            DrawDistrictPath(context, route, palette, focused);

        foreach (var route in landscape.Routes.Where(route => route.Kind == AtlasLandscapeRouteKind.CrossServiceDependency))
            DrawDependencyRoute(context, route, palette, focused);
    }

    private void DrawCompositionCorridor(
        DrawingContext context,
        AtlasLandscapeRoute route,
        ScenePalette palette,
        IReadOnlySet<string> focused)
    {
        var target = FindNode(route.TargetNodeId);
        if (target is null)
            return;

        var routeFocused = selectedNodeId is not null
            && focused.Contains(route.SourceNodeId)
            && focused.Contains(route.TargetNodeId);
        var dim = selectedNodeId is not null && !routeFocused;
        var accent = AccentFor(target, palette);
        var geometry = CreateRouteGeometry(route.Waypoints, smooth: theme != AtlasThemePreference.Machine);

        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(accent, dim ? (byte)4 : (byte)14)), Math.Max(5, 10 * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(accent, dim ? (byte)16 : routeFocused ? (byte)118 : (byte)50)), Math.Max(1, 1.7 * zoom)), geometry);
    }

    private void DrawDistrictPath(
        DrawingContext context,
        AtlasLandscapeRoute route,
        ScenePalette palette,
        IReadOnlySet<string> focused)
    {
        var source = FindNode(route.SourceNodeId);
        if (source is null)
            return;

        var routeFocused = selectedNodeId is not null
            && focused.Contains(route.SourceNodeId)
            && focused.Contains(route.TargetNodeId);
        var progressiveDetail = zoom >= 1.02 || routeFocused;
        if (!progressiveDetail)
            return;

        var accent = AccentFor(source, palette);
        var geometry = CreateRouteGeometry(route.Waypoints, smooth: theme != AtlasThemePreference.Machine);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(accent, routeFocused ? (byte)104 : (byte)46)), 1.0), geometry);
    }

    private void DrawDependencyRoute(
        DrawingContext context,
        AtlasLandscapeRoute route,
        ScenePalette palette,
        IReadOnlySet<string> focused)
    {
        var routeFocused = selectedNodeId is not null
            && focused.Contains(route.SourceNodeId)
            && focused.Contains(route.TargetNodeId);
        var dim = selectedNodeId is not null && !routeFocused;
        var geometry = CreateRouteGeometry(route.Waypoints, smooth: theme != AtlasThemePreference.Machine);
        var glowAlpha = dim ? (byte)6 : routeFocused ? (byte)44 : (byte)22;
        var lineAlpha = dim ? (byte)38 : routeFocused ? (byte)238 : (byte)145;

        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(palette.Dependency, glowAlpha)), Math.Max(5, 7 * zoom)), geometry);
        var linePen = new Pen(new SolidColorBrush(WithAlpha(palette.Dependency, lineAlpha)), Math.Max(1.1, 1.7 * zoom));
        context.DrawGeometry(null, linePen, geometry);

        var last = ScreenPoint(route.Waypoints[^1]);
        var previous = ScreenPoint(route.Waypoints[^2]);
        DrawArrowHead(context, previous, last, linePen);

        if (routeFocused && !reducedMotion)
            DrawRouteEnergy(context, route, palette.Dependency);
    }

    private void DrawGates(
        DrawingContext context,
        ScenePalette palette,
        IReadOnlySet<string> focused)
    {
        if (landscape is null)
            return;

        foreach (var gate in landscape.Gates)
        {
            var service = FindNode(gate.ServiceNodeId);
            if (service is null)
                continue;

            var accent = gate.Kind == AtlasLandscapeGateKind.NexusAccess
                ? AccentFor(service, palette)
                : palette.Dependency;
            var routeFocused = gate.RouteId is not null
                && landscape.Routes.FirstOrDefault(route => string.Equals(route.RouteId, gate.RouteId, StringComparison.Ordinal)) is { } route
                && selectedNodeId is not null
                && focused.Contains(route.SourceNodeId)
                && focused.Contains(route.TargetNodeId);
            var alpha = gate.Kind == AtlasLandscapeGateKind.NexusAccess
                ? (byte)82
                : routeFocused ? (byte)230 : (byte)126;

            DrawGateMarker(context, gate.Position, service, WithAlpha(accent, alpha));
        }
    }

    private void DrawGateMarker(
        DrawingContext context,
        Point worldPoint,
        AtlasNodePresentationViewModel service,
        Color color)
    {
        var point = ScreenPoint(worldPoint);
        var direction = Normalize(new Vector(service.X, service.Y));
        var lateral = new Vector(-direction.Y, direction.X);
        var pen = new Pen(new SolidColorBrush(color), 1.1);
        var half = Math.Max(5, 8 * zoom);
        var gap = Math.Max(3, 4 * zoom);

        var firstCenter = point + lateral * gap;
        var secondCenter = point - lateral * gap;
        context.DrawLine(pen, firstCenter - direction * half, firstCenter + direction * half);
        context.DrawLine(pen, secondCenter - direction * half, secondCenter + direction * half);
    }

    private void DrawCapabilityLandmarks(
        DrawingContext context,
        ScenePalette palette,
        IReadOnlySet<string> focused)
    {
        foreach (var capability in nodes.Where(node => node.IsCapability))
        {
            var selected = string.Equals(capability.NodeId, selectedNodeId, StringComparison.Ordinal);
            var contextual = selectedNodeId is null || focused.Contains(capability.NodeId);
            var showLabel = selected || (selectedNodeId is not null && focused.Contains(capability.NodeId)) || zoom >= 1.08;
            var point = ScreenPoint(new Point(capability.X, capability.Y));
            var accent = capability.IsAvailable ? AccentFor(capability, palette) : palette.Unavailable;

            using (context.PushOpacity(contextual ? 1 : 0.16))
            {
                var radius = Math.Max(4.2, 5.4 * zoom);
                context.DrawEllipse(
                    RadialGradient(WithAlpha(accent, selected ? (byte)210 : (byte)112), WithAlpha(accent, 0), 0.68),
                    null,
                    point,
                    radius + 8 * zoom,
                    radius + 8 * zoom);

                var diamond = CreateDiamond(point, selected ? radius + 2 : radius);
                context.DrawGeometry(
                    new SolidColorBrush(WithAlpha(accent, selected ? (byte)255 : (byte)220)),
                    selected ? new Pen(new SolidColorBrush(WithAlpha(palette.Text, 230)), 1) : null,
                    diamond);

                if (showLabel)
                {
                    var extendRight = capability.X >= 0;
                    DrawAnchoredText(
                        context,
                        capability.Title,
                        new Point(point.X + (extendRight ? 13 : -13) * zoom, point.Y - 8 * zoom),
                        extendRight,
                        Math.Clamp(9.2 * zoom, 8, 11),
                        WithAlpha(palette.Text, 224));
                }
            }
        }
    }

    private void DrawServiceLandmarks(
        DrawingContext context,
        ScenePalette palette,
        IReadOnlySet<string> focused)
    {
        foreach (var service in nodes.Where(node => node.IsService))
        {
            var selected = string.Equals(service.NodeId, selectedNodeId, StringComparison.Ordinal);
            var contextual = selectedNodeId is null || selected || focused.Contains(service.NodeId);
            var accent = AccentFor(service, palette);
            var center = ScreenPoint(new Point(service.X, service.Y));
            var radius = 36 * zoom;

            using (context.PushOpacity(contextual ? 1 : 0.26))
            {
                var pulse = selected ? SelectedPulse() : 1;
                context.DrawEllipse(
                    RadialGradient(WithAlpha(accent, selected ? (byte)96 : (byte)48), WithAlpha(accent, 0), 0.68),
                    null,
                    center,
                    (radius + 24 * zoom) * pulse,
                    (radius + 24 * zoom) * pulse);

                DrawLandmarkBody(context, center, radius, accent, palette, selected);
                DrawNodeSigil(context, service, center, radius * 0.56, new Pen(new SolidColorBrush(WithAlpha(accent, 235)), 1.65));
                DrawStatus(context, service, center, radius, palette);
                DrawCenteredText(context, service.Title, new Point(center.X, center.Y + radius + 9 * zoom), Math.Clamp(10.8 * zoom, 9, 13), palette.Text);
                DrawCenteredText(context, service.CompactStateText, new Point(center.X, center.Y + radius + 23 * zoom), Math.Clamp(7.4 * zoom, 6.8, 9), WithAlpha(palette.Muted, 205));
            }
        }
    }

    private void DrawCoreNexus(
        DrawingContext context,
        ScenePalette palette,
        IReadOnlySet<string> focused)
    {
        var core = nodes.FirstOrDefault(node => node.IsCore);
        if (core is null)
            return;

        var center = ScreenPoint(new Point(core.X, core.Y));
        var selected = string.Equals(core.NodeId, selectedNodeId, StringComparison.Ordinal);
        var radius = 47 * zoom;
        var pulse = selected ? SelectedPulse() : 1;

        context.DrawEllipse(
            RadialGradient(WithAlpha(palette.Core, selected ? (byte)116 : (byte)76), WithAlpha(palette.Core, 0), 0.72),
            null,
            center,
            (radius + 46 * zoom) * pulse,
            (radius + 40 * zoom) * pulse);

        context.DrawEllipse(
            new SolidColorBrush(WithAlpha(Mix(palette.RegionSurface, palette.Core, 0.22), 238)),
            new Pen(new SolidColorBrush(WithAlpha(palette.Core, 224)), selected ? 1.8 : 1.25),
            center,
            radius,
            radius);
        context.DrawEllipse(null, new Pen(new SolidColorBrush(WithAlpha(palette.Core, 72)), 1), center, radius + 15 * zoom, radius + 11 * zoom);
        context.DrawEllipse(null, new Pen(new SolidColorBrush(WithAlpha(palette.Core, 38)), 1), center, radius + 31 * zoom, radius + 24 * zoom);

        DrawNodeSigil(context, core, center, radius * 0.56, new Pen(new SolidColorBrush(WithAlpha(palette.Core, 244)), 2));
        DrawCenteredText(context, "Wiiii Got This", new Point(center.X, center.Y + radius + 10 * zoom), Math.Clamp(12.4 * zoom, 10, 15), palette.Text);
        DrawCenteredText(context, "SYSTEM NEXUS", new Point(center.X, center.Y + radius + 26 * zoom), Math.Clamp(7.4 * zoom, 6.8, 9), WithAlpha(palette.Core, 184));

        var gatePen = new Pen(new SolidColorBrush(WithAlpha(palette.Core, selectedNodeId is null || focused.Contains(core.NodeId) ? (byte)78 : (byte)30)), 1);
        foreach (var direction in new[] { new Vector(1, 0), new Vector(-1, 0), new Vector(0, 1), new Vector(0, -1) })
        {
            context.DrawLine(
                gatePen,
                center + direction * (radius + 7 * zoom),
                center + direction * (radius + 20 * zoom));
        }
    }

    private void DrawLandmarkBody(
        DrawingContext context,
        Point center,
        double radius,
        Color accent,
        ScenePalette palette,
        bool selected)
    {
        var fill = RadialGradient(
            WithAlpha(Mix(palette.RegionSurface, accent, 0.24), selected ? (byte)248 : (byte)235),
            WithAlpha(palette.RegionDepth, 242),
            0.78);
        var outline = new Pen(new SolidColorBrush(WithAlpha(accent, selected ? (byte)242 : (byte)170)), selected ? 1.7 : 1.05);

        if (theme == AtlasThemePreference.Machine)
        {
            var rect = new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2);
            context.FillRectangle(fill, rect);
            context.DrawRectangle(null, outline, rect);
            DrawMachineCorners(context, rect, 11 * zoom, new Pen(new SolidColorBrush(WithAlpha(accent, 214)), 1.35));
            return;
        }

        context.DrawEllipse(fill, outline, center, radius, theme == AtlasThemePreference.World ? radius * 0.91 : radius);
        if (theme == AtlasThemePreference.Elegant)
            context.DrawEllipse(null, new Pen(new SolidColorBrush(WithAlpha(palette.Secondary, 58)), 0.8), center, radius - 6 * zoom, radius - 6 * zoom);
    }

    private static void DrawNodeSigil(
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

    private static void DrawStatus(
        DrawingContext context,
        AtlasNodePresentationViewModel node,
        Point center,
        double radius,
        ScenePalette palette)
    {
        var color = node.IsAvailable ? palette.Available : palette.Unavailable;
        var point = new Point(center.X + radius * 0.72, center.Y - radius * 0.72);
        context.DrawEllipse(new SolidColorBrush(WithAlpha(color, 230)), null, point, 2.7, 2.7);
    }

    private void DrawRouteEnergy(DrawingContext context, AtlasLandscapeRoute route, Color color)
    {
        var screenPoints = route.Waypoints.Select(ScreenPoint).ToArray();
        var phase = AmbientPhase();
        for (var index = 0; index < 3; index++)
        {
            var point = PointAlongPolyline(screenPoints, (phase + index / 3d) % 1d);
            context.DrawEllipse(RadialGradient(WithAlpha(color, 210), WithAlpha(color, 0), 0.68), null, point, 7, 7);
            context.DrawEllipse(new SolidColorBrush(WithAlpha(color, 236)), null, point, 1.5, 1.5);
        }
    }

    private StreamGeometry CreateRegionGeometry(
        AtlasLandscapeRegion region,
        double scale,
        bool angular,
        Vector pixelOffset)
    {
        var points = region.Contour
            .Select(point =>
            {
                var relative = point - region.Landmark;
                var scaled = region.Landmark + relative * scale;
                return ScreenPoint(scaled) + pixelOffset;
            })
            .ToArray();

        var geometry = new StreamGeometry();
        using var geometryContext = geometry.Open();
        if (points.Length == 0)
            return geometry;

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

    private StreamGeometry CreateRouteGeometry(IReadOnlyList<Point> worldPoints, bool smooth)
    {
        var points = worldPoints.Select(ScreenPoint).ToArray();
        var geometry = new StreamGeometry();
        using var geometryContext = geometry.Open();
        if (points.Length == 0)
            return geometry;

        geometryContext.BeginFigure(points[0], isFilled: false);
        if (!smooth || points.Length < 3)
        {
            for (var index = 1; index < points.Length; index++)
                geometryContext.LineTo(points[index], isStroked: true);
        }
        else
        {
            for (var index = 1; index < points.Length - 1; index++)
            {
                var control = points[index];
                var end = MidPoint(points[index], points[index + 1]);
                geometryContext.QuadraticBezierTo(control, end, isStroked: true);
            }
            geometryContext.QuadraticBezierTo(points[^2], points[^1], isStroked: true);
        }
        geometryContext.EndFigure(isClosed: false);
        return geometry;
    }

    private static StreamGeometry CreateDiamond(Point center, double radius)
    {
        var geometry = new StreamGeometry();
        using var geometryContext = geometry.Open();
        geometryContext.BeginFigure(new Point(center.X, center.Y - radius), isFilled: true);
        geometryContext.LineTo(new Point(center.X + radius, center.Y), isStroked: true);
        geometryContext.LineTo(new Point(center.X, center.Y + radius), isStroked: true);
        geometryContext.LineTo(new Point(center.X - radius, center.Y), isStroked: true);
        geometryContext.EndFigure(isClosed: true);
        return geometry;
    }

    private bool RegionFocused(AtlasLandscapeRegion region, IReadOnlySet<string> focused)
    {
        if (selectedNodeId is null)
            return false;
        if (focused.Contains(region.NodeId))
            return true;
        return region.CapabilityNodeIds.Any(focused.Contains);
    }

    private AtlasNodePresentationViewModel? FindNode(string nodeId) =>
        nodes.FirstOrDefault(node => string.Equals(node.NodeId, nodeId, StringComparison.Ordinal));

    private AtlasNodePresentationViewModel? HitTestNode(Point screenPoint)
    {
        var focused = AtlasPresentationFocus.Build(connections, selectedNodeId);
        foreach (var node in nodes.OrderByDescending(node => node.IsCapability ? 1 : 2))
        {
            var center = ScreenPoint(new Point(node.X, node.Y));
            var radius = node.Kind switch
            {
                AtlasNodeKind.Core => Math.Max(39, 54 * zoom),
                AtlasNodeKind.Service => Math.Max(31, 45 * zoom),
                AtlasNodeKind.Capability when selectedNodeId is not null && focused.Contains(node.NodeId) => Math.Max(15, 21 * zoom),
                _ => Math.Max(10, 15 * zoom)
            };
            var delta = screenPoint - center;
            if (delta.X * delta.X + delta.Y * delta.Y <= radius * radius)
                return node;
        }
        return null;
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

    private Point ScreenPoint(Point worldPoint) =>
        ToScreen(WorldCenterX + worldPoint.X, WorldCenterY + worldPoint.Y);

    private Point ToScreen(double worldX, double worldY) =>
        new(worldX * zoom + translateX, worldY * zoom + translateY);

    private double ThemeTransitionProgress()
    {
        if (!themeTransitionActive || reducedMotion)
            return 1;

        var elapsed = DateTime.UtcNow - themeTransitionStartedUtc;
        if (elapsed >= ThemeTransitionDuration)
        {
            themeTransitionActive = false;
            previousTheme = theme;
            return 1;
        }

        var t = Math.Clamp(elapsed.TotalMilliseconds / ThemeTransitionDuration.TotalMilliseconds, 0, 1);
        return 1 - Math.Pow(1 - t, 3);
    }

    private double SelectedPulse()
    {
        if (reducedMotion)
            return 1;
        return 1 + Math.Sin(DateTime.UtcNow.TimeOfDay.TotalSeconds * Math.PI * 1.05) * 0.032;
    }

    private double AmbientPhase()
    {
        if (reducedMotion)
            return 0.45;
        return DateTime.UtcNow.TimeOfDay.TotalSeconds * 0.10 % 1;
    }

    private void RequestSceneFrame()
    {
        var animateSelection = !reducedMotion && selectedNodeId is not null;
        if (!themeTransitionActive && !animateSelection)
            return;
        TopLevel.GetTopLevel(this)?.RequestAnimationFrame(_ => InvalidateVisual());
    }

    private void DrawVignette(DrawingContext context, ScenePalette palette)
    {
        var brush = new RadialGradientBrush
        {
            Center = new RelativePoint(0.50, 0.48, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0.47, 0.43, RelativeUnit.Relative)
        };
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.65));
        brush.GradientStops.Add(new GradientStop(WithAlpha(palette.Shadow, 36), 0.85));
        brush.GradientStops.Add(new GradientStop(WithAlpha(palette.Shadow, 86), 1));
        context.FillRectangle(brush, new Rect(Bounds.Size));
    }

    private static Point PointAlongPolyline(Point[] points, double t)
    {
        if (points.Length == 0)
            return default;
        if (points.Length == 1)
            return points[0];

        var lengths = new double[points.Length - 1];
        var total = 0d;
        for (var index = 0; index < lengths.Length; index++)
        {
            var delta = points[index + 1] - points[index];
            lengths[index] = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
            total += lengths[index];
        }
        if (total < 0.001)
            return points[0];

        var target = Math.Clamp(t, 0, 1) * total;
        var traversed = 0d;
        for (var index = 0; index < lengths.Length; index++)
        {
            if (target <= traversed + lengths[index] || index == lengths.Length - 1)
            {
                var local = lengths[index] < 0.001 ? 0 : (target - traversed) / lengths[index];
                var start = points[index];
                var end = points[index + 1];
                return new Point(start.X + (end.X - start.X) * local, start.Y + (end.Y - start.Y) * local);
            }
            traversed += lengths[index];
        }
        return points[^1];
    }

    private static void DrawArrowHead(DrawingContext context, Point previous, Point end, Pen pen)
    {
        var tangent = end - previous;
        var length = Math.Sqrt(tangent.X * tangent.X + tangent.Y * tangent.Y);
        if (length < 0.01)
            return;
        var unit = new Vector(tangent.X / length, tangent.Y / length);
        var perpendicular = new Vector(-unit.Y, unit.X);
        var basePoint = end - unit * 9;
        context.DrawLine(pen, end, basePoint + perpendicular * 4);
        context.DrawLine(pen, end, basePoint - perpendicular * 4);
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
        context.DrawText(formatted, new Point(extendRight ? anchor.X : anchor.X - formatted.Width, anchor.Y));
    }

    private static void DrawCrosshair(DrawingContext context, Point center, double radius, IBrush brush)
    {
        var pen = new Pen(brush, 1);
        var tick = Math.Min(7, radius * 0.42);
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

    private static Vector Normalize(Vector vector)
    {
        var length = Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
        return length < 0.001 ? new Vector(0, -1) : new Vector(vector.X / length, vector.Y / length);
    }

    private static Point MidPoint(Point first, Point second) =>
        new((first.X + second.X) / 2, (first.Y + second.Y) / 2);

    private static double Mod(double value, double modulus)
    {
        if (modulus <= 0)
            return 0;
        var result = value % modulus;
        return result < 0 ? result + modulus : result;
    }

    private static LinearGradientBrush LinearGradient(Color start, Color end, RelativePoint startPoint, RelativePoint endPoint)
    {
        var brush = new LinearGradientBrush { StartPoint = startPoint, EndPoint = endPoint };
        brush.GradientStops.Add(new GradientStop(start, 0));
        brush.GradientStops.Add(new GradientStop(end, 1));
        return brush;
    }

    private static RadialGradientBrush RadialGradient(Color center, Color edge, double edgeOffset)
    {
        var brush = new RadialGradientBrush
        {
            Center = RelativePoint.Center,
            GradientOrigin = new RelativePoint(0.42, 0.36, RelativeUnit.Relative)
        };
        brush.GradientStops.Add(new GradientStop(center, 0));
        brush.GradientStops.Add(new GradientStop(Mix(center, edge, 0.62), Math.Clamp(edgeOffset * 0.58, 0.15, 0.78)));
        brush.GradientStops.Add(new GradientStop(edge, Math.Clamp(edgeOffset, 0.55, 1)));
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

    private readonly record struct ScenePalette(
        Color BackgroundTop,
        Color BackgroundBottom,
        Color RegionSurface,
        Color RegionDepth,
        Color Shadow,
        Color Primary,
        Color Secondary,
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
                Color.Parse("#171315"), Color.Parse("#0F0D10"), Color.Parse("#282124"), Color.Parse("#121012"), Color.Parse("#030203"),
                Color.Parse("#D7BDA5"), Color.Parse("#755E69"), Color.Parse("#E1B1A6"), Color.Parse("#F3E8DF"), Color.Parse("#A89590"),
                Color.Parse("#9CD4B4"), Color.Parse("#B87878"), Color.Parse("#E3CCB8"), Color.Parse("#B5D6C4"), Color.Parse("#E4C59A"),
                Color.Parse("#A9C4D6"), Color.Parse("#C3B3D8")),
            AtlasThemePreference.Machine => new(
                Color.Parse("#06110B"), Color.Parse("#020805"), Color.Parse("#0A2116"), Color.Parse("#031009"), Color.Parse("#000302"),
                Color.Parse("#72F1AE"), Color.Parse("#2A7651"), Color.Parse("#C2F285"), Color.Parse("#DFFFF0"), Color.Parse("#76A88B"),
                Color.Parse("#73EFAE"), Color.Parse("#D57272"), Color.Parse("#72F1AE"), Color.Parse("#79E3B2"), Color.Parse("#C4E987"),
                Color.Parse("#7BC9C1"), Color.Parse("#9CAE7B")),
            AtlasThemePreference.World => new(
                Color.Parse("#08140F"), Color.Parse("#030906"), Color.Parse("#10271D"), Color.Parse("#07150F"), Color.Parse("#000302"),
                Color.Parse("#89D8AD"), Color.Parse("#35624A"), Color.Parse("#D7D887"), Color.Parse("#E8F5EC"), Color.Parse("#84A58F"),
                Color.Parse("#92DFB5"), Color.Parse("#CF7676"), Color.Parse("#8DE8C2"), Color.Parse("#7ED6B2"), Color.Parse("#E1C46A"),
                Color.Parse("#75BEEA"), Color.Parse("#A395E6")),
            _ => new(
                Color.Parse("#07131A"), Color.Parse("#02080C"), Color.Parse("#0C202A"), Color.Parse("#061117"), Color.Parse("#000307"),
                Color.Parse("#68DDF5"), Color.Parse("#255E72"), Color.Parse("#A7CAFF"), Color.Parse("#E3FAFF"), Color.Parse("#789CA8"),
                Color.Parse("#6DE0C1"), Color.Parse("#D76F7B"), Color.Parse("#70E6EE"), Color.Parse("#62DDBB"), Color.Parse("#D6C879"),
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
