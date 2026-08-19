using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

/// <summary>
/// Living-world interpretation of the Atlas semantic projection.
/// Product providers become settlements, WGT becomes the containing central city,
/// shared infrastructure becomes a facility, and provider capabilities are revealed
/// as local places. This remains presentation-only and owns no provider semantics.
/// </summary>
public sealed class AtlasLivingWorldControl : Control
{
    private const double WorldCenterX = 1000d;
    private const double WorldCenterY = 660d;
    private const double CapabilityRevealZoom = 1.02d;
    private const double DetailRevealZoom = 0.72d;

    private static readonly WorldPalette Palette = new(
        BackgroundTop: Color.Parse("#071712"),
        BackgroundBottom: Color.Parse("#020907"),
        Ground: Color.Parse("#0B1C15"),
        GroundDeep: Color.Parse("#06110D"),
        GroundHighlight: Color.Parse("#153026"),
        Road: Color.Parse("#172820"),
        RoadEdge: Color.Parse("#315342"),
        RoadMark: Color.Parse("#6F907F"),
        Water: Color.Parse("#092524"),
        WaterHighlight: Color.Parse("#1B5450"),
        Text: Color.Parse("#EAF6EF"),
        Muted: Color.Parse("#86A496"),
        WarmLight: Color.Parse("#FFD58A"),
        CoolLight: Color.Parse("#8EE8DE"),
        Core: Color.Parse("#62E0B7"),
        Vocation: Color.Parse("#55D3A2"),
        Illumination: Color.Parse("#E3BF6C"),
        Orientation: Color.Parse("#6DB8E7"),
        Conveyance: Color.Parse("#A28AD9"),
        Generic: Color.Parse("#8CCDB0"),
        Unavailable: Color.Parse("#D57777"),
        Shadow: Color.Parse("#010302"));

    private readonly Dictionary<string, Point> worldPositions = new(StringComparer.Ordinal);
    private IReadOnlyList<AtlasNodePresentationViewModel> nodes = Array.Empty<AtlasNodePresentationViewModel>();
    private IReadOnlyList<AtlasConnectionPresentationViewModel> connections = Array.Empty<AtlasConnectionPresentationViewModel>();
    private string? selectedNodeId;
    private bool reducedMotion;
    private double zoom = 0.82d;
    private double translateX;
    private double translateY;

    public AtlasLivingWorldControl()
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
        bool nextReducedMotion)
    {
        nodes = nextNodes;
        connections = nextConnections;
        selectedNodeId = nextSelectedNodeId;
        reducedMotion = nextReducedMotion;
        RebuildWorldPositions();
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
        if (nodes.Count == 0)
            return;

        var focused = AtlasPresentationFocus.Build(connections, selectedNodeId);
        DrawNightLandscape(context);
        DrawTerrain(context, focused);
        DrawWatercourse(context);
        DrawRoadNetwork(context, focused);
        DrawDependencyRoutes(context, focused);
        DrawNaturalDetails(context);
        DrawProductSettlements(context, focused);
        DrawCapabilityPlaces(context, focused);
        DrawConveyanceFacility(context, focused);
        DrawWgtCity(context, focused);
        DrawAtmosphere(context);
        DrawVignette(context);
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

    private void RebuildWorldPositions()
    {
        worldPositions.Clear();

        var core = nodes.FirstOrDefault(node => node.IsCore);
        if (core is not null)
            worldPositions[core.NodeId] = default;

        var services = nodes
            .Where(node => node.IsService)
            .OrderBy(node => node.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(node => node.NodeId, StringComparer.Ordinal)
            .ToArray();

        SetKnownServicePosition(services, "vocation", new Point(-410, 80));
        SetKnownServicePosition(services, "illumination", new Point(-115, -350));
        SetKnownServicePosition(services, "orientation", new Point(405, -28));
        SetKnownServicePosition(services, "conveyance", new Point(190, 360));

        var occupied = worldPositions.Values.ToList();
        foreach (var service in services.Where(service => !worldPositions.ContainsKey(service.NodeId)))
        {
            var candidate = NextSettlementSlot(occupied);
            worldPositions[service.NodeId] = candidate;
            occupied.Add(candidate);
        }

        foreach (var service in services)
        {
            if (!worldPositions.TryGetValue(service.NodeId, out var settlement))
                continue;

            var capabilities = nodes
                .Where(node => node.IsCapability && node.ServiceIdentity == service.ServiceIdentity)
                .OrderBy(node => node.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(node => node.NodeId, StringComparer.Ordinal)
                .ToArray();

            for (var index = 0; index < capabilities.Length; index++)
                worldPositions[capabilities[index].NodeId] = CapabilityPosition(settlement, service, index, capabilities.Length);
        }
    }

    private void SetKnownServicePosition(
        AtlasNodePresentationViewModel[] services,
        string serviceId,
        Point position)
    {
        var service = services.FirstOrDefault(item => string.Equals(item.ServiceIdentity?.Value, serviceId, StringComparison.Ordinal));
        if (service is not null)
            worldPositions[service.NodeId] = position;
    }

    private static Point NextSettlementSlot(List<Point> occupied)
    {
        // Direct product regions remain viable into the low teens. Two rings are used
        // before grouping is even considered; grouping is a semantic product decision.
        for (var ring = 0; ring < 2; ring++)
        {
            var radius = ring == 0 ? 500d : 650d;
            var slotCount = ring == 0 ? 12 : 16;
            var phase = ring == 0 ? -Math.PI * 0.88 : -Math.PI * 0.79;
            for (var index = 0; index < slotCount; index++)
            {
                var angle = phase + Math.PI * 2 * index / slotCount;
                var candidate = new Point(
                    Math.Cos(angle) * radius,
                    Math.Sin(angle) * radius * 0.74);
                if (occupied.All(point => Distance(point, candidate) >= 185))
                    return candidate;
            }
        }

        var fallbackIndex = occupied.Count + 1;
        var fallbackAngle = -Math.PI / 2 + fallbackIndex * 2.399963229728653;
        var fallbackRadius = 680 + (fallbackIndex % 4) * 55;
        return new Point(
            Math.Cos(fallbackAngle) * fallbackRadius,
            Math.Sin(fallbackAngle) * fallbackRadius * 0.76);
    }

    private static Point CapabilityPosition(
        Point settlement,
        AtlasNodePresentationViewModel service,
        int index,
        int count)
    {
        var serviceDirection = Normalize(new Vector(settlement.X, settlement.Y));
        var lateral = new Vector(-serviceDirection.Y, serviceDirection.X);
        var slots = Math.Max(1, count);
        var spread = Math.Min(110d, 30d * Math.Max(0, slots - 1));
        var lateralOffset = slots == 1 ? 0 : -spread / 2 + spread * index / (slots - 1);
        var forward = 84 + (index % 2) * 22;

        if (string.Equals(service.ServiceIdentity?.Value, "conveyance", StringComparison.Ordinal))
            return settlement + new Vector(-74 + index * 42, -22 - (index % 2) * 26);

        return settlement + serviceDirection * forward + lateral * lateralOffset;
    }

    private void DrawNightLandscape(DrawingContext context)
    {
        context.FillRectangle(
            LinearGradient(
                Palette.BackgroundTop,
                Palette.BackgroundBottom,
                new RelativePoint(0.08, 0.02, RelativeUnit.Relative),
                new RelativePoint(0.92, 0.98, RelativeUnit.Relative)),
            new Rect(Bounds.Size));

        var city = ScreenPoint(default);
        context.DrawEllipse(
            RadialGradient(WithAlpha(Palette.Core, 30), WithAlpha(Palette.Core, 0), 0.78),
            null,
            city,
            330 * zoom,
            255 * zoom);
    }

    private void DrawTerrain(DrawingContext context, IReadOnlySet<string> focused)
    {
        foreach (var service in ProductServices())
        {
            if (!worldPositions.TryGetValue(service.NodeId, out var center))
                continue;

            var accent = AccentFor(service);
            var selected = string.Equals(service.NodeId, selectedNodeId, StringComparison.Ordinal);
            var contextual = selectedNodeId is null || selected || focused.Contains(service.NodeId);
            var patch = CreateOrganicGroundPatch(center, service.ServiceIdentity?.Value);
            var geometry = CreateWorldGeometry(patch, smooth: true, isFilled: true);

            using (context.PushOpacity(contextual ? 1 : 0.42))
            {
                context.DrawGeometry(
                    LinearGradient(
                        WithAlpha(Mix(Palette.Ground, accent, 0.10), service.IsAvailable ? (byte)235 : (byte)218),
                        WithAlpha(Palette.GroundDeep, 238),
                        new RelativePoint(0.18, 0.08, RelativeUnit.Relative),
                        new RelativePoint(0.82, 0.92, RelativeUnit.Relative)),
                    new Pen(new SolidColorBrush(WithAlpha(accent, selected ? (byte)74 : (byte)30)), selected ? 1.35 : 0.85),
                    geometry);

                DrawContour(context, center, service.ServiceIdentity?.Value, accent, 0.74, 18);
                DrawContour(context, center, service.ServiceIdentity?.Value, accent, 0.49, 12);
            }
        }

        var coreGround = CreateWorldGeometry(
        [
            new(-175, -106), new(-72, -148), new(66, -138), new(165, -82),
            new(188, 28), new(126, 125), new(12, 154), new(-116, 128), new(-194, 42)
        ], smooth: true, isFilled: true);
        context.DrawGeometry(
            LinearGradient(
                WithAlpha(Mix(Palette.GroundHighlight, Palette.Core, 0.14), 235),
                WithAlpha(Palette.GroundDeep, 240),
                new RelativePoint(0.24, 0.08, RelativeUnit.Relative),
                new RelativePoint(0.82, 0.94, RelativeUnit.Relative)),
            new Pen(new SolidColorBrush(WithAlpha(Palette.Core, 40)), 0.9),
            coreGround);
    }

    private void DrawContour(
        DrawingContext context,
        Point center,
        string? serviceId,
        Color accent,
        double scale,
        byte alpha)
    {
        var patch = CreateOrganicGroundPatch(center, serviceId)
            .Select(point => center + (point - center) * scale)
            .ToArray();
        context.DrawGeometry(
            null,
            new Pen(new SolidColorBrush(WithAlpha(accent, alpha)), 0.8),
            CreateWorldGeometry(patch, smooth: true, isFilled: false, close: true));
    }

    private void DrawWatercourse(DrawingContext context)
    {
        var points = new[]
        {
            new Point(-760, 250), new Point(-560, 215), new Point(-360, 248),
            new Point(-132, 226), new Point(80, 252), new Point(305, 238),
            new Point(540, 205), new Point(760, 230)
        };
        var geometry = CreateWorldGeometry(points, smooth: true, isFilled: false, close: false);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Palette.Water, 170)), Math.Max(11, 18 * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Palette.WaterHighlight, 42)), Math.Max(1, 1.2 * zoom)), geometry);
    }

    private void DrawRoadNetwork(DrawingContext context, IReadOnlySet<string> focused)
    {
        var products = ProductServices().ToArray();
        if (products.Length == 0)
            return;

        if (products.Length >= 8)
            DrawInnerDistributorRoad(context);

        foreach (var service in products)
        {
            if (!worldPositions.TryGetValue(service.NodeId, out var target))
                continue;

            var selected = string.Equals(service.NodeId, selectedNodeId, StringComparison.Ordinal);
            var contextual = selectedNodeId is null || selected || focused.Contains(service.NodeId);
            var direction = Normalize(new Vector(target.X, target.Y));
            var startDistance = products.Length >= 8 ? 135d : 88d;
            var start = new Point(direction.X * startDistance, direction.Y * startDistance);
            var end = target - direction * 84;
            var bend = new Vector(-direction.Y, direction.X) * StableRoadBend(service.NodeId);
            var control = MidPoint(start, end) + bend;
            DrawRoad(context, start, control, end, contextual, selected);
        }
    }

    private void DrawInnerDistributorRoad(DrawingContext context)
    {
        var ring = new[]
        {
            new Point(-138, -50), new Point(-76, -122), new Point(35, -132), new Point(128, -72),
            new Point(146, 28), new Point(82, 118), new Point(-28, 136), new Point(-124, 72)
        };
        var geometry = CreateWorldGeometry(ring, smooth: true, isFilled: false, close: true);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Palette.Shadow, 150)), Math.Max(9, 13 * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Palette.Road, 245)), Math.Max(6, 9 * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Palette.RoadEdge, 110)), Math.Max(0.8, 1 * zoom)), geometry);
    }

    private void DrawRoad(
        DrawingContext context,
        Point start,
        Point control,
        Point end,
        bool contextual,
        bool selected)
    {
        using (context.PushOpacity(contextual ? 1 : 0.30))
        {
            var geometry = CreateQuadraticWorldRoute(start, control, end);
            context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Palette.Shadow, 180)), Math.Max(11, 15 * zoom)), geometry);
            context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Palette.Road, 250)), Math.Max(7, 10 * zoom)), geometry);
            context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Palette.RoadEdge, selected ? (byte)168 : (byte)88)), Math.Max(1, 1.25 * zoom)), geometry);

            if (zoom >= 0.92)
                DrawRoadDashes(context, start, control, end, selected ? (byte)122 : (byte)70);
        }
    }

    private void DrawRoadDashes(DrawingContext context, Point start, Point control, Point end, byte alpha)
    {
        var pen = new Pen(new SolidColorBrush(WithAlpha(Palette.RoadMark, alpha)), Math.Max(0.7, 0.9 * zoom));
        for (var index = 1; index <= 5; index++)
        {
            var t = index / 6d;
            var point = QuadraticPoint(start, control, end, t);
            var next = QuadraticPoint(start, control, end, Math.Min(1, t + 0.025));
            var tangent = Normalize(next - point);
            context.DrawLine(pen, ScreenPoint(point - tangent * 4), ScreenPoint(point + tangent * 4));
        }
    }

    private void DrawDependencyRoutes(DrawingContext context, IReadOnlySet<string> focused)
    {
        foreach (var connection in connections.Where(item => item.Kind == AtlasConnectionKind.CapabilityDependency))
        {
            if (!worldPositions.TryGetValue(connection.Source.NodeId, out var source)
                || !worldPositions.TryGetValue(connection.Target.NodeId, out var target))
            {
                continue;
            }

            var routeFocused = selectedNodeId is not null
                && focused.Contains(connection.Source.NodeId)
                && focused.Contains(connection.Target.NodeId);
            if (zoom < 0.90 && !routeFocused)
                continue;

            var sourceTown = nodes.FirstOrDefault(node =>
                node.IsService && node.ServiceIdentity == connection.Source.ServiceIdentity);
            var sourceTownPoint = sourceTown is not null && worldPositions.TryGetValue(sourceTown.NodeId, out var townPosition)
                ? townPosition
                : source;
            var outward = Normalize(target - sourceTownPoint);
            var side = new Vector(-outward.Y, outward.X);
            var control = MidPoint(source, target) + side * 74;
            var geometry = CreateQuadraticWorldRoute(source, control, target);

            using (context.PushOpacity(selectedNodeId is null || routeFocused ? 1 : 0.28))
            {
                context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Palette.Orientation, routeFocused ? (byte)66 : (byte)28)), Math.Max(5, 7 * zoom)), geometry);
                var pen = new Pen(new SolidColorBrush(WithAlpha(Palette.Orientation, routeFocused ? (byte)232 : (byte)132)), Math.Max(1.1, 1.5 * zoom));
                context.DrawGeometry(null, pen, geometry);
                DrawRouteArrow(context, control, target, pen);
                if (routeFocused && !reducedMotion)
                    DrawRouteEnergy(context, source, control, target, Palette.Orientation);
            }
        }
    }

    private void DrawNaturalDetails(DrawingContext context)
    {
        foreach (var service in ProductServices())
        {
            if (!worldPositions.TryGetValue(service.NodeId, out var center))
                continue;

            var seed = StableHash(service.NodeId);
            for (var index = 0; index < 8; index++)
            {
                var angle = ((seed % 31) * 0.07 + index * 2.399963229728653) % (Math.PI * 2);
                var radius = 118 + (index % 3) * 28;
                var tree = center + new Vector(Math.Cos(angle) * radius, Math.Sin(angle) * radius * 0.68);
                DrawTree(context, tree, 7 + (index % 2) * 2);
            }
        }

        DrawTreeCluster(context, new Point(-225, -210), 5);
        DrawTreeCluster(context, new Point(245, -205), 6);
        DrawTreeCluster(context, new Point(-250, 285), 4);
        DrawTreeCluster(context, new Point(430, 250), 5);
    }

    private void DrawTreeCluster(DrawingContext context, Point center, int count)
    {
        for (var index = 0; index < count; index++)
        {
            var offset = new Vector((index % 3) * 18 - 18, (index / 3) * 16 - 8);
            DrawTree(context, center + offset, 7 + index % 3);
        }
    }

    private void DrawTree(DrawingContext context, Point worldPoint, double size)
    {
        if (zoom < 0.60)
            return;

        var point = ScreenPoint(worldPoint);
        var trunk = new Pen(new SolidColorBrush(Color.Parse("#604C35")), Math.Max(0.8, 1.2 * zoom));
        context.DrawLine(trunk, point, new Point(point.X, point.Y + size * 1.35 * zoom));
        var shadow = new Point(point.X + 3 * zoom, point.Y + 5 * zoom);
        context.DrawEllipse(new SolidColorBrush(WithAlpha(Palette.Shadow, 76)), null, shadow, size * 0.95 * zoom, size * 0.58 * zoom);
        context.DrawEllipse(new SolidColorBrush(Color.Parse("#153D2A")), null, new Point(point.X - 2 * zoom, point.Y), size * 0.78 * zoom, size * 0.92 * zoom);
        context.DrawEllipse(new SolidColorBrush(Color.Parse("#2C6646")), null, new Point(point.X + 3 * zoom, point.Y - 3 * zoom), size * 0.58 * zoom, size * 0.66 * zoom);
    }

    private void DrawProductSettlements(DrawingContext context, IReadOnlySet<string> focused)
    {
        foreach (var service in ProductServices())
        {
            if (!worldPositions.TryGetValue(service.NodeId, out var center))
                continue;

            var selected = string.Equals(service.NodeId, selectedNodeId, StringComparison.Ordinal);
            var contextual = selectedNodeId is null || selected || focused.Contains(service.NodeId);
            var accent = AccentFor(service);
            using (context.PushOpacity(contextual ? 1 : 0.36))
            {
                DrawSettlementLight(context, center, accent, selected, service.IsAvailable);
                DrawTownSquare(context, center, accent);

                switch (service.ServiceIdentity?.Value)
                {
                    case "vocation":
                        DrawVocationTown(context, center, accent, service.IsAvailable);
                        break;
                    case "illumination":
                        DrawIlluminationVillage(context, center, accent, service.IsAvailable);
                        break;
                    case "orientation":
                        DrawOrientationTown(context, center, accent, service.IsAvailable);
                        break;
                    default:
                        DrawGenericTown(context, center, accent, service.IsAvailable);
                        break;
                }

                DrawSettlementStatus(context, service, center, accent, selected);
            }
        }
    }

    private void DrawTownSquare(DrawingContext context, Point center, Color accent)
    {
        var point = ScreenPoint(center);
        var rect = new Rect(point.X - 35 * zoom, point.Y - 20 * zoom, 70 * zoom, 40 * zoom);
        context.FillRectangle(new SolidColorBrush(WithAlpha(Mix(Palette.Road, accent, 0.10), 240)), rect);
        context.DrawRectangle(null, new Pen(new SolidColorBrush(WithAlpha(accent, 54)), 0.8), rect);
        DrawStreetLamp(context, center + new Vector(-29, -17), accent);
        DrawStreetLamp(context, center + new Vector(29, 17), accent);
    }

    private void DrawVocationTown(DrawingContext context, Point center, Color accent, bool available)
    {
        DrawBuilding(context, center + new Vector(-58, -42), 34, 31, 12, accent, 0.86, available, 3);
        DrawBuilding(context, center + new Vector(-15, -58), 38, 44, 14, accent, 0.74, available, 4);
        DrawBuilding(context, center + new Vector(42, -39), 31, 29, 11, accent, 0.68, available, 2);
        DrawBuilding(context, center + new Vector(60, 21), 42, 34, 13, accent, 0.62, available, 3);
        DrawBuilding(context, center + new Vector(-47, 31), 40, 27, 12, accent, 0.58, available, 2);
        if (zoom >= DetailRevealZoom)
            DrawBuilding(context, center + new Vector(10, 48), 30, 23, 9, accent, 0.52, available, 2);
    }

    private void DrawIlluminationVillage(DrawingContext context, Point center, Color accent, bool available)
    {
        DrawBuilding(context, center + new Vector(-47, -34), 32, 27, 11, accent, 0.68, available, 3);
        DrawBuilding(context, center + new Vector(0, -55), 39, 52, 13, accent, 0.88, available, 5);
        DrawBuilding(context, center + new Vector(49, -25), 31, 30, 10, accent, 0.62, available, 3);
        DrawBuilding(context, center + new Vector(-49, 32), 36, 25, 11, accent, 0.55, available, 2);
        DrawBuilding(context, center + new Vector(43, 35), 34, 28, 10, accent, 0.58, available, 3);
        DrawBeacon(context, center + new Vector(0, -86), accent, available);
    }

    private void DrawOrientationTown(DrawingContext context, Point center, Color accent, bool available)
    {
        DrawBuilding(context, center + new Vector(-54, -35), 35, 27, 11, accent, 0.58, available, 2);
        DrawBuilding(context, center + new Vector(-9, -51), 36, 41, 12, accent, 0.72, available, 4);
        DrawBuilding(context, center + new Vector(45, -32), 31, 31, 10, accent, 0.66, available, 3);
        DrawBuilding(context, center + new Vector(-42, 34), 34, 25, 10, accent, 0.52, available, 2);
        DrawBuilding(context, center + new Vector(44, 31), 39, 29, 11, accent, 0.57, available, 2);
        DrawWayfinder(context, center + new Vector(71, -4), accent, available);
    }

    private void DrawGenericTown(DrawingContext context, Point center, Color accent, bool available)
    {
        DrawBuilding(context, center + new Vector(-46, -34), 33, 29, 11, accent, 0.64, available, 2);
        DrawBuilding(context, center + new Vector(0, -48), 36, 39, 12, accent, 0.76, available, 4);
        DrawBuilding(context, center + new Vector(47, -28), 31, 27, 10, accent, 0.58, available, 2);
        DrawBuilding(context, center + new Vector(-38, 35), 36, 26, 11, accent, 0.54, available, 2);
        DrawBuilding(context, center + new Vector(39, 34), 34, 28, 10, accent, 0.58, available, 2);
    }

    private void DrawCapabilityPlaces(DrawingContext context, IReadOnlySet<string> focused)
    {
        foreach (var capability in nodes.Where(node => node.IsCapability))
        {
            var selected = string.Equals(capability.NodeId, selectedNodeId, StringComparison.Ordinal);
            var contextual = selectedNodeId is not null && focused.Contains(capability.NodeId);
            if (zoom < CapabilityRevealZoom && !selected && !contextual)
                continue;
            if (!worldPositions.TryGetValue(capability.NodeId, out var position))
                continue;

            var accent = capability.IsAvailable ? AccentFor(capability) : Palette.Unavailable;
            using (context.PushOpacity(selectedNodeId is null || contextual || selected ? 1 : 0.28))
                DrawCapabilityBuilding(context, capability, position, accent, selected);
        }
    }

    private void DrawCapabilityBuilding(
        DrawingContext context,
        AtlasNodePresentationViewModel capability,
        Point position,
        Color accent,
        bool selected)
    {
        if (selected)
        {
            var point = ScreenPoint(position);
            context.DrawEllipse(
                RadialGradient(WithAlpha(accent, 82), WithAlpha(accent, 0), 0.72),
                null,
                point,
                43 * zoom * SelectionPulse(),
                32 * zoom * SelectionPulse());
        }

        DrawBuilding(context, position, 27, selected ? 37 : 31, 9, accent, 0.88, capability.IsAvailable, 3);
        DrawBeacon(context, position + new Vector(0, -27), accent, capability.IsAvailable);
        var screen = ScreenPoint(position);
        DrawCenteredText(
            context,
            capability.Title,
            new Point(screen.X, screen.Y + 24 * zoom),
            Math.Clamp(8.8 * zoom, 7.6, 10.4),
            WithAlpha(Palette.Text, 226));
    }

    private void DrawConveyanceFacility(DrawingContext context, IReadOnlySet<string> focused)
    {
        var service = nodes.FirstOrDefault(node => node.IsSharedCapabilityProvider);
        if (service is null || !worldPositions.TryGetValue(service.NodeId, out var center))
            return;

        var selected = string.Equals(service.NodeId, selectedNodeId, StringComparison.Ordinal);
        var contextual = selectedNodeId is null || selected || focused.Contains(service.NodeId);
        var accent = AccentFor(service);
        using (context.PushOpacity(contextual ? 1 : 0.34))
        {
            DrawIndustrialGround(context, center, accent, selected);
            DrawWarehouse(context, center + new Vector(-62, -24), 68, 31, accent, service.IsAvailable);
            DrawWarehouse(context, center + new Vector(16, -15), 58, 28, accent, service.IsAvailable);
            DrawWarehouse(context, center + new Vector(-31, 35), 61, 25, accent, service.IsAvailable);
            DrawSilo(context, center + new Vector(58, 30), 14, 30, accent, service.IsAvailable);
            DrawSilo(context, center + new Vector(86, 24), 12, 25, accent, service.IsAvailable);
            DrawRelayMast(context, center + new Vector(77, -35), accent, service.IsAvailable);
            DrawFacilityStatus(context, service, center, accent, selected);
        }
    }

    private void DrawIndustrialGround(DrawingContext context, Point center, Color accent, bool selected)
    {
        var points = new[]
        {
            center + new Vector(-132, -82), center + new Vector(92, -96), center + new Vector(142, -38),
            center + new Vector(132, 77), center + new Vector(24, 103), center + new Vector(-118, 76)
        };
        context.DrawGeometry(
            new SolidColorBrush(WithAlpha(Mix(Palette.GroundDeep, accent, 0.13), 236)),
            new Pen(new SolidColorBrush(WithAlpha(accent, selected ? (byte)88 : (byte)34)), 1),
            CreateWorldGeometry(points, smooth: false, isFilled: true));

        var roadPen = new Pen(new SolidColorBrush(WithAlpha(Palette.RoadEdge, 76)), Math.Max(1, 1.3 * zoom));
        context.DrawLine(roadPen, ScreenPoint(center + new Vector(-118, 3)), ScreenPoint(center + new Vector(126, 3)));
        context.DrawLine(roadPen, ScreenPoint(center + new Vector(-8, -82)), ScreenPoint(center + new Vector(-8, 82)));
    }

    private void DrawWgtCity(DrawingContext context, IReadOnlySet<string> focused)
    {
        var core = nodes.FirstOrDefault(node => node.IsCore);
        if (core is null)
            return;

        var selected = string.Equals(core.NodeId, selectedNodeId, StringComparison.Ordinal);
        var contextual = selectedNodeId is null || selected || focused.Contains(core.NodeId);
        using (context.PushOpacity(contextual ? 1 : 0.58))
        {
            DrawCityStreetGrid(context);
            DrawSettlementLight(context, default, Palette.Core, selected, true, 210, 156);
            DrawBuilding(context, new Point(-92, -57), 43, 48, 15, Palette.Core, 0.52, true, 5);
            DrawBuilding(context, new Point(-42, -82), 38, 66, 14, Palette.Core, 0.60, true, 6);
            DrawBuilding(context, new Point(20, -91), 45, 80, 16, Palette.Core, 0.72, true, 7);
            DrawBuilding(context, new Point(83, -61), 39, 55, 14, Palette.Core, 0.57, true, 5);
            DrawBuilding(context, new Point(-102, 13), 46, 41, 15, Palette.Core, 0.46, true, 4);
            DrawBuilding(context, new Point(97, 8), 45, 45, 15, Palette.Core, 0.50, true, 4);
            DrawBuilding(context, new Point(-73, 68), 42, 44, 14, Palette.Core, 0.48, true, 4);
            DrawBuilding(context, new Point(72, 69), 43, 46, 14, Palette.Core, 0.52, true, 4);
            DrawCoreTower(context, selected);
            DrawStreetLamp(context, new Point(-43, 17), Palette.Core);
            DrawStreetLamp(context, new Point(46, 18), Palette.Core);
            DrawStreetLamp(context, new Point(0, 64), Palette.Core);

            var center = ScreenPoint(default);
            DrawCenteredText(context, "WIIII GOT THIS", new Point(center.X, center.Y + 118 * zoom), Math.Clamp(13.2 * zoom, 10.5, 15.5), Palette.Text);
            DrawCenteredText(context, "CENTRAL CITY", new Point(center.X, center.Y + 136 * zoom), Math.Clamp(7.7 * zoom, 6.8, 9.4), WithAlpha(Palette.Core, 190));
        }
    }

    private void DrawCityStreetGrid(DrawingContext context)
    {
        var road = new Pen(new SolidColorBrush(WithAlpha(Palette.Road, 250)), Math.Max(8, 11 * zoom));
        var edge = new Pen(new SolidColorBrush(WithAlpha(Palette.RoadEdge, 88)), Math.Max(0.8, 1.1 * zoom));
        var paths = new[]
        {
            (new Point(-134, -20), new Point(134, -20)),
            (new Point(-125, 45), new Point(124, 45)),
            (new Point(-20, -132), new Point(-20, 132)),
            (new Point(48, -118), new Point(48, 118))
        };
        foreach (var (start, end) in paths)
        {
            context.DrawLine(road, ScreenPoint(start), ScreenPoint(end));
            context.DrawLine(edge, ScreenPoint(start), ScreenPoint(end));
        }
    }

    private void DrawCoreTower(DrawingContext context, bool selected)
    {
        var center = new Point(10, 10);
        if (selected)
        {
            var point = ScreenPoint(center);
            context.DrawEllipse(
                RadialGradient(WithAlpha(Palette.Core, 108), WithAlpha(Palette.Core, 0), 0.72),
                null,
                point,
                72 * zoom * SelectionPulse(),
                58 * zoom * SelectionPulse());
        }

        DrawBuilding(context, center, 54, 112, 19, Palette.Core, 0.94, true, 9);
        DrawBeacon(context, center + new Vector(0, -70), Palette.Core, true);
    }

    private void DrawSettlementStatus(
        DrawingContext context,
        AtlasNodePresentationViewModel service,
        Point center,
        Color accent,
        bool selected)
    {
        var labelPoint = ScreenPoint(center + new Vector(0, 92));
        DrawCenteredText(context, service.Title, labelPoint, Math.Clamp(11.2 * zoom, 9, 13.4), Palette.Text);
        DrawCenteredText(
            context,
            service.CompactStateText,
            new Point(labelPoint.X, labelPoint.Y + 15 * zoom),
            Math.Clamp(7.1 * zoom, 6.5, 8.7),
            WithAlpha(service.IsAvailable ? accent : Palette.Muted, selected ? (byte)225 : (byte)175));
    }

    private void DrawFacilityStatus(
        DrawingContext context,
        AtlasNodePresentationViewModel service,
        Point center,
        Color accent,
        bool selected)
    {
        var labelPoint = ScreenPoint(center + new Vector(0, 118));
        DrawCenteredText(context, service.Title.ToUpperInvariant(), labelPoint, Math.Clamp(10.5 * zoom, 8.8, 12.8), Palette.Text);
        DrawCenteredText(
            context,
            "RELAY YARD · " + service.CompactStateText,
            new Point(labelPoint.X, labelPoint.Y + 15 * zoom),
            Math.Clamp(7 * zoom, 6.4, 8.6),
            WithAlpha(accent, selected ? (byte)224 : (byte)170));
    }

    private void DrawSettlementLight(
        DrawingContext context,
        Point center,
        Color accent,
        bool selected,
        bool available,
        double radiusX = 150,
        double radiusY = 108)
    {
        var screen = ScreenPoint(center);
        var alpha = available ? selected ? (byte)72 : (byte)44 : (byte)22;
        context.DrawEllipse(
            RadialGradient(WithAlpha(accent, alpha), WithAlpha(accent, 0), 0.76),
            null,
            screen,
            radiusX * zoom,
            radiusY * zoom);
    }

    private void DrawBuilding(
        DrawingContext context,
        Point baseWorld,
        double width,
        double height,
        double depth,
        Color accent,
        double accentMix,
        bool powered,
        int windowRows)
    {
        var basePoint = ScreenPoint(baseWorld);
        var w = width * zoom;
        var h = height * zoom;
        var d = depth * zoom;
        var topY = basePoint.Y - h;
        var left = basePoint.X - w / 2;
        var right = basePoint.X + w / 2;
        var roofLift = d * 0.46;
        var roofShift = d * 0.62;

        context.DrawGeometry(
            new SolidColorBrush(WithAlpha(Palette.Shadow, 118)),
            null,
            Polygon([
                new(left + 5 * zoom, basePoint.Y + 5 * zoom),
                new(right + 12 * zoom, basePoint.Y + 5 * zoom),
                new(right + 21 * zoom, basePoint.Y + 12 * zoom),
                new(left + 12 * zoom, basePoint.Y + 12 * zoom)
            ]));

        var body = Mix(Color.Parse("#173127"), accent, accentMix * 0.22);
        context.DrawGeometry(
            new SolidColorBrush(WithAlpha(body, 248)),
            new Pen(new SolidColorBrush(WithAlpha(accent, 82)), 0.8),
            Polygon([new(left, topY), new(right, topY), new(right, basePoint.Y), new(left, basePoint.Y)]));
        context.DrawGeometry(
            new SolidColorBrush(WithAlpha(Mix(body, Palette.Shadow, 0.28), 248)),
            null,
            Polygon([
                new(right, topY), new(right + roofShift, topY - roofLift),
                new(right + roofShift, basePoint.Y - roofLift), new(right, basePoint.Y)
            ]));
        context.DrawGeometry(
            new SolidColorBrush(WithAlpha(Mix(body, accent, 0.28), 252)),
            new Pen(new SolidColorBrush(WithAlpha(accent, 92)), 0.75),
            Polygon([
                new(left, topY), new(left + roofShift, topY - roofLift),
                new(right + roofShift, topY - roofLift), new(right, topY)
            ]));

        if (zoom < DetailRevealZoom)
            return;

        var rows = Math.Clamp(windowRows, 1, 9);
        var cols = width >= 42 ? 3 : 2;
        var windowColor = powered ? Palette.WarmLight : Palette.Muted;
        for (var row = 0; row < rows; row++)
        {
            var y = topY + (row + 1) * h / (rows + 1);
            for (var col = 0; col < cols; col++)
            {
                var x = left + (col + 1) * w / (cols + 1);
                context.FillRectangle(
                    new SolidColorBrush(WithAlpha(windowColor, powered ? (byte)205 : (byte)72)),
                    new Rect(x - 1.8 * zoom, y - 1.1 * zoom, 3.6 * zoom, 2.2 * zoom));
            }
        }
    }

    private void DrawWarehouse(DrawingContext context, Point baseWorld, double width, double height, Color accent, bool powered)
    {
        DrawBuilding(context, baseWorld, width, height, 12, accent, 0.36, powered, 1);
        if (zoom < DetailRevealZoom)
            return;

        var point = ScreenPoint(baseWorld);
        var door = new Rect(
            point.X - width * zoom * 0.13,
            point.Y - height * zoom * 0.42,
            width * zoom * 0.26,
            height * zoom * 0.42);
        context.FillRectangle(new SolidColorBrush(WithAlpha(Palette.Shadow, 190)), door);
        context.DrawRectangle(null, new Pen(new SolidColorBrush(WithAlpha(accent, 82)), 0.8), door);
    }

    private void DrawSilo(DrawingContext context, Point baseWorld, double radius, double height, Color accent, bool powered)
    {
        var point = ScreenPoint(baseWorld);
        var r = radius * zoom;
        var h = height * zoom;
        var rect = new Rect(point.X - r, point.Y - h, r * 2, h);
        context.FillRectangle(new SolidColorBrush(WithAlpha(Mix(Palette.GroundHighlight, accent, 0.18), 242)), rect);
        context.DrawRectangle(null, new Pen(new SolidColorBrush(WithAlpha(accent, 92)), 0.8), rect);
        context.DrawEllipse(new SolidColorBrush(WithAlpha(Mix(Palette.GroundHighlight, accent, 0.28), 248)), null, new Point(point.X, point.Y - h), r, r * 0.36);
        if (powered)
            context.DrawEllipse(new SolidColorBrush(WithAlpha(Palette.WarmLight, 190)), null, new Point(point.X + r * 0.45, point.Y - h * 0.72), 1.5 * zoom, 1.5 * zoom);
    }

    private void DrawRelayMast(DrawingContext context, Point worldPoint, Color accent, bool powered)
    {
        var point = ScreenPoint(worldPoint);
        var pen = new Pen(new SolidColorBrush(WithAlpha(accent, powered ? (byte)214 : (byte)92)), Math.Max(0.9, 1.1 * zoom));
        context.DrawLine(pen, new Point(point.X, point.Y - 52 * zoom), new Point(point.X - 12 * zoom, point.Y));
        context.DrawLine(pen, new Point(point.X, point.Y - 52 * zoom), new Point(point.X + 12 * zoom, point.Y));
        context.DrawLine(pen, new Point(point.X - 9 * zoom, point.Y - 18 * zoom), new Point(point.X + 9 * zoom, point.Y - 18 * zoom));
        context.DrawLine(pen, new Point(point.X - 6 * zoom, point.Y - 34 * zoom), new Point(point.X + 6 * zoom, point.Y - 34 * zoom));
        if (!powered)
            return;

        context.DrawEllipse(
            RadialGradient(WithAlpha(accent, 82), WithAlpha(accent, 0), 0.72),
            null,
            new Point(point.X, point.Y - 54 * zoom),
            18 * zoom,
            18 * zoom);
        context.DrawEllipse(new SolidColorBrush(WithAlpha(accent, 240)), null, new Point(point.X, point.Y - 54 * zoom), 2 * zoom, 2 * zoom);
    }

    private void DrawBeacon(DrawingContext context, Point worldPoint, Color accent, bool powered)
    {
        var point = ScreenPoint(worldPoint);
        var pen = new Pen(new SolidColorBrush(WithAlpha(accent, powered ? (byte)190 : (byte)76)), Math.Max(0.8, 1 * zoom));
        context.DrawLine(pen, point, new Point(point.X, point.Y - 13 * zoom));
        if (!powered)
            return;

        var light = new Point(point.X, point.Y - 15 * zoom);
        context.DrawEllipse(RadialGradient(WithAlpha(accent, 78), WithAlpha(accent, 0), 0.72), null, light, 9 * zoom, 9 * zoom);
        context.DrawEllipse(new SolidColorBrush(WithAlpha(accent, 235)), null, light, 1.5 * zoom, 1.5 * zoom);
    }

    private void DrawWayfinder(DrawingContext context, Point worldPoint, Color accent, bool powered)
    {
        var point = ScreenPoint(worldPoint);
        var pen = new Pen(new SolidColorBrush(WithAlpha(accent, powered ? (byte)205 : (byte)86)), Math.Max(0.9, 1.1 * zoom));
        context.DrawLine(pen, new Point(point.X, point.Y - 37 * zoom), new Point(point.X, point.Y + 8 * zoom));
        context.DrawLine(pen, new Point(point.X, point.Y - 28 * zoom), new Point(point.X + 18 * zoom, point.Y - 28 * zoom));
        context.DrawLine(pen, new Point(point.X, point.Y - 15 * zoom), new Point(point.X - 15 * zoom, point.Y - 15 * zoom));
        if (powered)
            context.DrawEllipse(new SolidColorBrush(WithAlpha(accent, 225)), null, new Point(point.X, point.Y - 39 * zoom), 2 * zoom, 2 * zoom);
    }

    private void DrawStreetLamp(DrawingContext context, Point worldPoint, Color accent)
    {
        if (zoom < 0.66)
            return;

        var point = ScreenPoint(worldPoint);
        context.DrawLine(
            new Pen(new SolidColorBrush(WithAlpha(Palette.Muted, 118)), Math.Max(0.6, 0.8 * zoom)),
            point,
            new Point(point.X, point.Y - 11 * zoom));
        var light = new Point(point.X, point.Y - 12 * zoom);
        context.DrawEllipse(
            RadialGradient(WithAlpha(Mix(Palette.WarmLight, accent, 0.18), 58), WithAlpha(Palette.WarmLight, 0), 0.72),
            null,
            light,
            12 * zoom,
            12 * zoom);
        context.DrawEllipse(new SolidColorBrush(WithAlpha(Palette.WarmLight, 220)), null, light, 1.2 * zoom, 1.2 * zoom);
    }

    private void DrawAtmosphere(DrawingContext context)
    {
        if (reducedMotion)
            return;

        var phase = DateTime.UtcNow.TimeOfDay.TotalSeconds * 0.018;
        for (var index = 0; index < 9; index++)
        {
            var x = (index * 173 + phase * 31) % Math.Max(1, Bounds.Width + 120) - 60;
            var y = (index * 97 + Math.Sin(phase + index) * 24 + Bounds.Height * 0.18) % Math.Max(1, Bounds.Height);
            context.DrawEllipse(
                new SolidColorBrush(WithAlpha(Palette.CoolLight, (byte)(18 + index % 3 * 8))),
                null,
                new Point(x, y),
                1.1,
                1.1);
        }
    }

    private void DrawVignette(DrawingContext context)
    {
        var brush = new RadialGradientBrush
        {
            Center = new RelativePoint(0.50, 0.46, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0.47, 0.42, RelativeUnit.Relative)
        };
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.67));
        brush.GradientStops.Add(new GradientStop(WithAlpha(Palette.Shadow, 42), 0.86));
        brush.GradientStops.Add(new GradientStop(WithAlpha(Palette.Shadow, 102), 1));
        context.FillRectangle(brush, new Rect(Bounds.Size));
    }

    private IEnumerable<AtlasNodePresentationViewModel> ProductServices() =>
        nodes.Where(node => node.IsPrimaryProductProvider);

    private AtlasNodePresentationViewModel? HitTestNode(Point screenPoint)
    {
        var focused = AtlasPresentationFocus.Build(connections, selectedNodeId);
        foreach (var node in nodes.OrderByDescending(node => node.IsCapability ? 3 : node.IsCore ? 2 : 1))
        {
            if (!worldPositions.TryGetValue(node.NodeId, out var worldPoint))
                continue;
            if (node.IsCapability
                && zoom < CapabilityRevealZoom
                && !string.Equals(node.NodeId, selectedNodeId, StringComparison.Ordinal)
                && !focused.Contains(node.NodeId))
            {
                continue;
            }

            var center = ScreenPoint(worldPoint);
            var radius = node.Kind switch
            {
                AtlasNodeKind.Core => Math.Max(42, 72 * zoom),
                AtlasNodeKind.Service when node.IsSharedCapabilityProvider => Math.Max(40, 86 * zoom),
                AtlasNodeKind.Service => Math.Max(38, 76 * zoom),
                AtlasNodeKind.Capability => Math.Max(14, 24 * zoom),
                _ => Math.Max(12, 18 * zoom)
            };
            var delta = screenPoint - center;
            if (delta.X * delta.X + delta.Y * delta.Y <= radius * radius)
                return node;
        }
        return null;
    }

    private static Color AccentFor(AtlasNodePresentationViewModel node)
    {
        if (node.IsCore)
            return Palette.Core;
        return node.ServiceIdentity?.Value switch
        {
            "vocation" => Palette.Vocation,
            "illumination" => Palette.Illumination,
            "orientation" => Palette.Orientation,
            "conveyance" => Palette.Conveyance,
            _ => Palette.Generic
        };
    }

    private static Point[] CreateOrganicGroundPatch(Point center, string? serviceId)
    {
        var seed = StableHash(serviceId ?? "generic");
        var rx = string.Equals(serviceId, "vocation", StringComparison.Ordinal) ? 180d : 160d;
        var ry = string.Equals(serviceId, "illumination", StringComparison.Ordinal) ? 132d : 118d;
        var points = new Point[11];
        for (var index = 0; index < points.Length; index++)
        {
            var angle = Math.PI * 2 * index / points.Length;
            var variation = 0.84 + ((seed >> (index % 8)) & 3) * 0.055;
            points[index] = center + new Vector(
                Math.Cos(angle) * rx * variation,
                Math.Sin(angle) * ry * (0.92 + (index % 3) * 0.035));
        }
        return points;
    }

    private StreamGeometry CreateWorldGeometry(Point[] worldPoints, bool smooth, bool isFilled, bool close = true)
    {
        var points = worldPoints.Select(ScreenPoint).ToArray();
        var geometry = new StreamGeometry();
        using var geometryContext = geometry.Open();
        if (points.Length == 0)
            return geometry;

        if (!smooth || points.Length < 3)
        {
            geometryContext.BeginFigure(points[0], isFilled);
            for (var index = 1; index < points.Length; index++)
                geometryContext.LineTo(points[index], isStroked: true);
            geometryContext.EndFigure(isClosed: close);
            return geometry;
        }

        geometryContext.BeginFigure(close ? MidPoint(points[^1], points[0]) : points[0], isFilled);
        if (close)
        {
            for (var index = 0; index < points.Length; index++)
            {
                var current = points[index];
                var next = points[(index + 1) % points.Length];
                geometryContext.QuadraticBezierTo(current, MidPoint(current, next), isStroked: true);
            }
        }
        else
        {
            for (var index = 1; index < points.Length - 1; index++)
            {
                var current = points[index];
                var next = points[index + 1];
                geometryContext.QuadraticBezierTo(current, MidPoint(current, next), isStroked: true);
            }
            geometryContext.LineTo(points[^1], isStroked: true);
        }
        geometryContext.EndFigure(isClosed: close);
        return geometry;
    }

    private StreamGeometry CreateQuadraticWorldRoute(Point start, Point control, Point end)
    {
        var geometry = new StreamGeometry();
        using var geometryContext = geometry.Open();
        geometryContext.BeginFigure(ScreenPoint(start), isFilled: false);
        geometryContext.QuadraticBezierTo(ScreenPoint(control), ScreenPoint(end), isStroked: true);
        geometryContext.EndFigure(isClosed: false);
        return geometry;
    }

    private static StreamGeometry Polygon(Point[] points)
    {
        var geometry = new StreamGeometry();
        using var geometryContext = geometry.Open();
        if (points.Length == 0)
            return geometry;
        geometryContext.BeginFigure(points[0], isFilled: true);
        for (var index = 1; index < points.Length; index++)
            geometryContext.LineTo(points[index], isStroked: true);
        geometryContext.EndFigure(isClosed: true);
        return geometry;
    }

    private void DrawRouteArrow(DrawingContext context, Point previousWorld, Point endWorld, Pen pen)
    {
        var previous = ScreenPoint(previousWorld);
        var end = ScreenPoint(endWorld);
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

    private void DrawRouteEnergy(DrawingContext context, Point start, Point control, Point end, Color color)
    {
        var phase = DateTime.UtcNow.TimeOfDay.TotalSeconds * 0.11 % 1;
        for (var index = 0; index < 3; index++)
        {
            var t = (phase + index / 3d) % 1d;
            var point = ScreenPoint(QuadraticPoint(start, control, end, t));
            context.DrawEllipse(RadialGradient(WithAlpha(color, 200), WithAlpha(color, 0), 0.68), null, point, 7, 7);
            context.DrawEllipse(new SolidColorBrush(WithAlpha(color, 235)), null, point, 1.4, 1.4);
        }
    }

    private double SelectionPulse()
    {
        if (reducedMotion)
            return 1;
        return 1 + Math.Sin(DateTime.UtcNow.TimeOfDay.TotalSeconds * Math.PI * 0.9) * 0.035;
    }

    private void RequestSceneFrame()
    {
        if (reducedMotion || selectedNodeId is null)
            return;
        TopLevel.GetTopLevel(this)?.RequestAnimationFrame(_ => InvalidateVisual());
    }

    private Point ScreenPoint(Point worldPoint) =>
        new(
            (WorldCenterX + worldPoint.X) * zoom + translateX,
            (WorldCenterY + worldPoint.Y) * zoom + translateY);

    private static Point QuadraticPoint(Point start, Point control, Point end, double t)
    {
        var inverse = 1 - t;
        return new Point(
            inverse * inverse * start.X + 2 * inverse * t * control.X + t * t * end.X,
            inverse * inverse * start.Y + 2 * inverse * t * control.Y + t * t * end.Y);
    }

    private static double StableRoadBend(string value) =>
        ((StableHash(value) % 5) - 2) * 12d;

    private static int StableHash(string value)
    {
        var hash = 17;
        foreach (var character in value)
            hash = unchecked(hash * 31 + character);
        return Math.Abs(hash == int.MinValue ? int.MaxValue : hash);
    }

    private static double Distance(Point first, Point second)
    {
        var delta = first - second;
        return Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
    }

    private static Vector Normalize(Vector vector)
    {
        var length = Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
        return length < 0.001 ? new Vector(0, -1) : new Vector(vector.X / length, vector.Y / length);
    }

    private static Point MidPoint(Point first, Point second) =>
        new((first.X + second.X) / 2, (first.Y + second.Y) / 2);

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
        brush.GradientStops.Add(new GradientStop(Mix(center, edge, 0.60), Math.Clamp(edgeOffset * 0.56, 0.15, 0.78)));
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

    private readonly record struct WorldPalette(
        Color BackgroundTop,
        Color BackgroundBottom,
        Color Ground,
        Color GroundDeep,
        Color GroundHighlight,
        Color Road,
        Color RoadEdge,
        Color RoadMark,
        Color Water,
        Color WaterHighlight,
        Color Text,
        Color Muted,
        Color WarmLight,
        Color CoolLight,
        Color Core,
        Color Vocation,
        Color Illumination,
        Color Orientation,
        Color Conveyance,
        Color Generic,
        Color Unavailable,
        Color Shadow);
}
