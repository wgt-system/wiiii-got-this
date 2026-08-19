using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

/// <summary>
/// Release-direction World interpretation of the Atlas projection.
///
/// This control deliberately does not expose repository/process topology. First-class products
/// become authored settlements, shared capabilities become facilities/networks, and WGT becomes
/// the containing city. The semantic source remains AtlasProjection; this class owns presentation
/// only and may be replaced without changing provider/domain ownership.
/// </summary>
public sealed class AtlasWorldV2Control : Control
{
    private const double WorldCenterX = 1000d;
    private const double WorldCenterY = 660d;
    private const double MidDetailZoom = 0.78d;
    private const double CloseDetailZoom = 1.08d;

    private static readonly IReadOnlyDictionary<string, Point> AuthoredServicePositions =
        new Dictionary<string, Point>(StringComparer.Ordinal)
        {
            ["vocation"] = new(-465, 155),
            ["illumination"] = new(-235, -330),
            ["orientation"] = new(430, -145),
            ["conveyance"] = new(445, 300)
        };

    // Direct products remain deliberately possible into the low teens without inventing groups.
    // These are authored fallbacks, not a force/radial layout.
    private static readonly Point[] AdditionalProductSlots =
    [
        new(-650, -130), new(650, 90), new(-610, 390), new(120, -475),
        new(680, -360), new(-130, 430), new(-760, 175), new(760, 315),
        new(-465, -455), new(365, 470), new(40, 525), new(760, -55)
    ];

    private static readonly Point[] MainLand =
    [
        new(-800, -510), new(-520, -570), new(-190, -535), new(75, -570),
        new(410, -535), new(745, -390), new(820, -105), new(780, 170),
        new(690, 470), new(390, 555), new(90, 530), new(-210, 585),
        new(-520, 520), new(-760, 325), new(-835, 40), new(-820, -245)
    ];

    private static readonly Point[] WgtPark =
    [new(-75, -65), new(60, -95), new(205, -30), new(190, 95), new(70, 145), new(-70, 105)];

    private static readonly Point[] VocationFields =
    [new(-710, 160), new(-575, 70), new(-485, 130), new(-555, 285), new(-700, 315)];

    private static readonly Point[] IlluminationGarden =
    [new(-400, -430), new(-220, -470), new(-95, -385), new(-155, -250), new(-330, -255), new(-430, -335)];

    private static readonly Point[] OrientationRidge =
    [new(260, -300), new(475, -345), new(650, -235), new(610, -85), new(390, -45), new(250, -135)];

    private static readonly Color BackgroundTop = Color.Parse("#06130F");
    private static readonly Color BackgroundBottom = Color.Parse("#020806");
    private static readonly Color Land = Color.Parse("#10231A");
    private static readonly Color LandDeep = Color.Parse("#091711");
    private static readonly Color LandEdge = Color.Parse("#29523C");
    private static readonly Color Road = Color.Parse("#26382E");
    private static readonly Color RoadEdge = Color.Parse("#55705E");
    private static readonly Color Water = Color.Parse("#0B2E30");
    private static readonly Color WaterLight = Color.Parse("#286368");
    private static readonly Color Rail = Color.Parse("#566159");
    private static readonly Color Text = Color.Parse("#E9F5EE");
    private static readonly Color Muted = Color.Parse("#8FA99A");
    private static readonly Color Window = Color.Parse("#F5C878");
    private static readonly Color Core = Color.Parse("#60E0B5");
    private static readonly Color Vocation = Color.Parse("#4DD09A");
    private static readonly Color Illumination = Color.Parse("#E1B95B");
    private static readonly Color Orientation = Color.Parse("#68B8E7");
    private static readonly Color Conveyance = Color.Parse("#A185D3");
    private static readonly Color Shadow = Color.Parse("#010302");

    private IReadOnlyList<AtlasNodePresentationViewModel> nodes = Array.Empty<AtlasNodePresentationViewModel>();
    private IReadOnlyList<AtlasConnectionPresentationViewModel> connections = Array.Empty<AtlasConnectionPresentationViewModel>();
    private readonly Dictionary<string, Point> positions = new(StringComparer.Ordinal);
    private string? selectedNodeId;
    private bool reducedMotion;
    private double zoom = 0.82d;
    private double translateX;
    private double translateY;

    public AtlasWorldV2Control()
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
        RebuildPositions();
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
        DrawBackground(context);
        DrawLand(context);
        DrawRegionalGround(context);
        DrawRiver(context);
        DrawPrimaryRoads(context);
        DrawRailAndLogistics(context);
        DrawVegetation(context);
        DrawVocation(context, focused);
        DrawIllumination(context, focused);
        DrawOrientation(context, focused);
        DrawAdditionalProducts(context, focused);
        DrawWgtCity(context, focused);
        DrawSharedInfrastructure(context, focused);
        DrawSemanticNetworks(context, focused);
        DrawAtmosphere(context);
        DrawVignette(context);
        RequestSceneFrame();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var current = e.GetCurrentPoint(this);
        if (!current.Properties.IsLeftButtonPressed)
            return;

        var node = HitTestProduct(e.GetPosition(this));
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
            && nodes.FirstOrDefault(node => string.Equals(node.NodeId, selectedNodeId, StringComparison.Ordinal)) is { } selected)
        {
            if (selected.CanOpenProductSurface)
                NodeActivated?.Invoke(selected);
            else
                NodeInvoked?.Invoke(selected);
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private void RebuildPositions()
    {
        positions.Clear();
        var core = nodes.FirstOrDefault(node => node.IsCore);
        if (core is not null)
            positions[core.NodeId] = new Point(55, 25);

        var usedAdditionalSlots = 0;
        foreach (var service in nodes.Where(node => node.IsService).OrderBy(node => node.Title, StringComparer.OrdinalIgnoreCase))
        {
            var serviceId = service.ServiceIdentity?.Value;
            if (serviceId is not null && AuthoredServicePositions.TryGetValue(serviceId, out var known))
            {
                positions[service.NodeId] = known;
                continue;
            }

            positions[service.NodeId] = AdditionalProductSlots[Math.Min(usedAdditionalSlots, AdditionalProductSlots.Length - 1)];
            usedAdditionalSlots++;
        }

        foreach (var capability in nodes.Where(node => node.IsCapability))
        {
            var owner = nodes.FirstOrDefault(candidate =>
                candidate.IsService && candidate.ServiceIdentity == capability.ServiceIdentity);
            if (owner is null || !positions.TryGetValue(owner.NodeId, out var ownerPoint))
                continue;

            positions[capability.NodeId] = ownerPoint + CapabilityLocalOffset(capability);
        }
    }

    private static Vector CapabilityLocalOffset(AtlasNodePresentationViewModel capability)
    {
        var identity = capability.CapabilityIdentity?.Value;
        if (string.Equals(identity, BuildAtlasProjectionUseCase.OrientationGeospatialCapabilityId, StringComparison.Ordinal))
            return new Vector(-74, 56);
        if (string.Equals(identity, BuildAtlasProjectionUseCase.ConveyanceDurableDeliveryCapabilityId, StringComparison.Ordinal))
            return new Vector(-84, -28);

        var hash = StableHash(capability.NodeId);
        return new Vector(-58 + hash % 116, 48 + hash % 42);
    }

    private void DrawBackground(DrawingContext context)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative)
        };
        brush.GradientStops.Add(new GradientStop(BackgroundTop, 0));
        brush.GradientStops.Add(new GradientStop(BackgroundBottom, 1));
        context.FillRectangle(brush, new Rect(Bounds.Size));
    }

    private void DrawLand(DrawingContext context)
    {
        var geometry = WorldGeometry(MainLand, smooth: true, filled: true);
        context.DrawGeometry(new SolidColorBrush(LandDeep), null, geometry);

        var inner = MainLand.Select(point => new Point(point.X * 0.975 + 5, point.Y * 0.965 - 4)).ToArray();
        context.DrawGeometry(
            new SolidColorBrush(WithAlpha(Land, 244)),
            new Pen(new SolidColorBrush(WithAlpha(LandEdge, 92)), Math.Max(0.8, 1.1 * zoom)),
            WorldGeometry(inner, smooth: true, filled: true));

        if (zoom >= MidDetailZoom)
        {
            DrawContour(context, [new(-710, -390), new(-370, -455), new(-35, -430), new(285, -455), new(645, -315)], 26);
            DrawContour(context, [new(-735, 350), new(-430, 440), new(-70, 425), new(250, 455), new(605, 365)], 18);
        }
    }

    private void DrawRegionalGround(DrawingContext context)
    {
        context.DrawGeometry(new SolidColorBrush(Color.Parse("#152A1B")), null, WorldGeometry(VocationFields, smooth: true, filled: true));
        context.DrawGeometry(new SolidColorBrush(Color.Parse("#18281A")), null, WorldGeometry(IlluminationGarden, smooth: true, filled: true));
        context.DrawGeometry(new SolidColorBrush(Color.Parse("#10252A")), null, WorldGeometry(OrientationRidge, smooth: true, filled: true));
        context.DrawGeometry(new SolidColorBrush(Color.Parse("#143022")), null, WorldGeometry(WgtPark, smooth: true, filled: true));

        if (zoom >= MidDetailZoom)
        {
            DrawFieldRows(context);
            DrawCampusPaths(context);
            DrawOrientationContours(context);
        }
    }

    private void DrawFieldRows(DrawingContext context)
    {
        var pen = new Pen(new SolidColorBrush(Color.Parse("#315238")), Math.Max(0.5, 0.7 * zoom));
        for (var index = 0; index < 7; index++)
        {
            var y = 150 + index * 20;
            context.DrawLine(pen, Screen(new Point(-675, y)), Screen(new Point(-555, y - 38)));
        }
    }

    private void DrawCampusPaths(DrawingContext context)
    {
        var pen = new Pen(new SolidColorBrush(WithAlpha(Color.Parse("#827553"), 100)), Math.Max(1, 1.3 * zoom));
        context.DrawGeometry(null, pen, WorldRoute([new(-395, -370), new(-330, -325), new(-245, -340), new(-170, -290)]));
        context.DrawGeometry(null, pen, WorldRoute([new(-335, -445), new(-285, -390), new(-210, -405), new(-135, -350)]));
    }

    private void DrawOrientationContours(DrawingContext context)
    {
        var pen = new Pen(new SolidColorBrush(WithAlpha(Orientation, 46)), Math.Max(0.6, 0.8 * zoom));
        foreach (var inset in new[] { 0d, 24d, 46d })
        {
            var points = OrientationRidge
                .Select(point => new Point(430 + (point.X - 430) * (1 - inset / 330), -175 + (point.Y + 175) * (1 - inset / 240)))
                .ToArray();
            context.DrawGeometry(null, pen, WorldGeometry(points, smooth: true, filled: false));
        }
    }

    private void DrawRiver(DrawingContext context)
    {
        var river = new[]
        {
            new Point(-840, -70), new(-620, -25), new(-390, -40), new(-155, 20),
            new(30, 105), new(250, 130), new(470, 105), new(710, 160), new(845, 250)
        };
        var geometry = WorldRoute(river);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Shadow, 135)), Math.Max(16, 22 * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(Water), Math.Max(11, 16 * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(WaterLight, 100)), Math.Max(1, 1.4 * zoom)), geometry);

        // Small tributary through the knowledge gardens.
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(WaterLight, 74)), Math.Max(2, 3 * zoom)),
            WorldRoute([new(-470, -515), new(-405, -445), new(-360, -360), new(-330, -285), new(-345, -205)]));
    }

    private void DrawPrimaryRoads(DrawingContext context)
    {
        DrawRoad(context, [new(-485, 135), new(-360, 70), new(-180, 65), new(-35, 32), new(35, 24)]);
        DrawRoad(context, [new(-225, -310), new(-185, -205), new(-95, -125), new(15, -55), new(48, 10)]);
        DrawRoad(context, [new(95, 5), new(220, -10), new(310, -75), new(425, -130)]);
        DrawRoad(context, [new(105, 55), new(235, 145), new(335, 220), new(438, 285)]);

        // A southern bypass prevents every route from terminating visually in the city centre.
        DrawRoad(context, [new(-500, 230), new(-260, 330), new(10, 345), new(250, 325), new(465, 300)], secondary: true);
    }

    private void DrawRoad(DrawingContext context, Point[] points, bool secondary = false)
    {
        var geometry = WorldRoute(points);
        var shadowWidth = secondary ? 9d : 14d;
        var roadWidth = secondary ? 5d : 9d;
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Shadow, 175)), Math.Max(5, shadowWidth * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(Road), Math.Max(3, roadWidth * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(RoadEdge, secondary ? (byte)60 : (byte)92)), Math.Max(0.6, 0.9 * zoom)), geometry);
    }

    private void DrawRailAndLogistics(DrawingContext context)
    {
        var railPath = new[] { new Point(-755, 390), new(-470, 375), new(-150, 400), new(145, 385), new(440, 305), new(725, 355) };
        var geometry = WorldRoute(railPath);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Shadow, 170)), Math.Max(4, 6 * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Rail, 185)), Math.Max(1.6, 2.2 * zoom)), geometry);

        if (zoom < MidDetailZoom)
            return;
        for (var index = 0; index < 15; index++)
        {
            var t = index / 14d;
            var point = SamplePolyline(railPath, t);
            var next = SamplePolyline(railPath, Math.Min(1, t + 0.015));
            var tangent = Normalize(next - point);
            var normal = new Vector(-tangent.Y, tangent.X);
            context.DrawLine(new Pen(new SolidColorBrush(WithAlpha(Rail, 150)), Math.Max(0.6, 0.8 * zoom)),
                Screen(point - normal * 5), Screen(point + normal * 5));
        }
    }

    private void DrawVegetation(DrawingContext context)
    {
        var trees = new[]
        {
            new Point(-735,-315), new(-700,-290), new(-665,-330), new(-600,-390), new(-540,-430),
            new(-470,-470), new(-420,-205), new(-365,-195), new(-315,-210), new(-125,-500),
            new(-70,-485), new(10,-505), new(155,-455), new(215,-430), new(280,-405),
            new(675,-125), new(705,-75), new(675,5), new(715,70), new(650,210),
            new(600,425), new(540,455), new(355,485), new(250,500), new(80,480),
            new(-65,500), new(-210,510), new(-355,475), new(-610,405), new(-700,340)
        };
        foreach (var tree in trees)
            DrawTree(context, tree, 8);

        if (zoom < MidDetailZoom)
            return;
        foreach (var tree in new[] { new Point(-560,110), new(-530,90), new(-505,110), new(-280,-275), new(-250,-260), new(180,80), new(210,100), new(250,70) })
            DrawTree(context, tree, 6);
    }

    private void DrawVocation(DrawingContext context, IReadOnlySet<string> focused)
    {
        if (!TryService("vocation", out var service, out var center))
            return;
        using (context.PushOpacity(ContextOpacity(service, focused)))
        {
            DrawLocalLight(context, center, Vocation, service.IsAvailable, 150, 100);
            DrawTownRoad(context, [center + new Vector(-105, 10), center + new Vector(-15, -8), center + new Vector(105, 12)]);
            DrawTownRoad(context, [center + new Vector(-25, -95), center + new Vector(-15, -8), center + new Vector(5, 94)]);

            DrawPitchedBuilding(context, center + new Vector(-85, -28), 58, 35, Vocation, service.IsAvailable);
            DrawCivicBuilding(context, center + new Vector(-20, -38), 50, 58, Vocation, service.IsAvailable);
            DrawPitchedBuilding(context, center + new Vector(66, -18), 52, 32, Vocation, service.IsAvailable);
            DrawLongHall(context, center + new Vector(72, 55), 84, 31, Vocation, service.IsAvailable);
            DrawPitchedBuilding(context, center + new Vector(-70, 60), 47, 28, Vocation, service.IsAvailable);

            if (zoom >= CloseDetailZoom)
            {
                DrawMarketCanopy(context, center + new Vector(0, 38), Vocation);
                DrawTree(context, center + new Vector(-8, 55), 7);
                DrawTree(context, center + new Vector(19, 56), 7);
            }

            DrawPlaceLabel(context, "Vocation", center + new Vector(0, 116), Vocation, service.IsAvailable);
        }
    }

    private void DrawIllumination(DrawingContext context, IReadOnlySet<string> focused)
    {
        if (!TryService("illumination", out var service, out var center))
            return;
        using (context.PushOpacity(ContextOpacity(service, focused)))
        {
            DrawLocalLight(context, center, Illumination, service.IsAvailable, 145, 110);
            DrawCampusCourt(context, center, Illumination);
            DrawLibraryWing(context, center + new Vector(-80, 10), 78, 30, Illumination, service.IsAvailable);
            DrawLibraryWing(context, center + new Vector(78, 14), 72, 29, Illumination, service.IsAvailable);
            DrawLanternHall(context, center + new Vector(0, -28), Illumination, service.IsAvailable);
            DrawPavilion(context, center + new Vector(-58, 67), Illumination, service.IsAvailable);
            DrawPavilion(context, center + new Vector(60, 67), Illumination, service.IsAvailable);

            if (zoom >= CloseDetailZoom)
                DrawArcade(context, center + new Vector(0, 52), Illumination);

            DrawPlaceLabel(context, "Illumination", center + new Vector(0, 120), Illumination, service.IsAvailable);
        }
    }

    private void DrawOrientation(DrawingContext context, IReadOnlySet<string> focused)
    {
        if (!TryService("orientation", out var service, out var center))
            return;
        using (context.PushOpacity(ContextOpacity(service, focused)))
        {
            DrawLocalLight(context, center, Orientation, service.IsAvailable, 150, 110);
            DrawSurveyPlaza(context, center, Orientation);
            DrawSurveyPavilion(context, center + new Vector(-72, 22), Orientation, service.IsAvailable);
            DrawObservationTower(context, center + new Vector(24, -8), Orientation, service.IsAvailable);
            DrawPavilion(context, center + new Vector(82, 49), Orientation, service.IsAvailable);
            DrawSurveyMast(context, center + new Vector(105, -18), Orientation, service.IsAvailable);

            if (zoom >= CloseDetailZoom)
                DrawBearingRose(context, center + new Vector(-7, 65), Orientation);

            DrawPlaceLabel(context, "Orientation", center + new Vector(0, 122), Orientation, service.IsAvailable);
        }
    }

    private void DrawAdditionalProducts(DrawingContext context, IReadOnlySet<string> focused)
    {
        foreach (var service in nodes.Where(node => node.IsPrimaryProductProvider && !IsKnownProduct(node)))
        {
            if (!positions.TryGetValue(service.NodeId, out var center))
                continue;
            using (context.PushOpacity(ContextOpacity(service, focused)))
            {
                DrawLocalLight(context, center, Core, service.IsAvailable, 120, 88);
                DrawPitchedBuilding(context, center + new Vector(-35, 0), 46, 30, Core, service.IsAvailable);
                DrawCivicBuilding(context, center + new Vector(30, -5), 43, 47, Core, service.IsAvailable);
                DrawPlaceLabel(context, service.Title, center + new Vector(0, 78), Core, service.IsAvailable);
            }
        }
    }

    private void DrawWgtCity(DrawingContext context, IReadOnlySet<string> focused)
    {
        var coreNode = nodes.FirstOrDefault(node => node.IsCore);
        if (coreNode is null || !positions.TryGetValue(coreNode.NodeId, out var center))
            return;
        using (context.PushOpacity(ContextOpacity(coreNode, focused)))
        {
            DrawLocalLight(context, center, Core, available: true, 245, 165);
            DrawCityAvenues(context, center);
            DrawCityBlock(context, center + new Vector(-125, -75), 58, 72, Core, 4);
            DrawCityBlock(context, center + new Vector(-55, -105), 62, 105, Core, 6);
            DrawCityBlock(context, center + new Vector(40, -118), 72, 142, Core, 8);
            DrawCityBlock(context, center + new Vector(125, -72), 60, 82, Core, 5);
            DrawCityBlock(context, center + new Vector(-130, 25), 69, 58, Core, 3);
            DrawCityBlock(context, center + new Vector(-45, 45), 72, 78, Core, 4);
            DrawCityBlock(context, center + new Vector(53, 50), 78, 92, Core, 5);
            DrawCityBlock(context, center + new Vector(142, 32), 65, 61, Core, 3);
            DrawCentralTower(context, center + new Vector(34, -2), Core);
            DrawPlaceLabel(context, "WIIII GOT THIS", center + new Vector(8, 176), Core, true, isCore: true);
        }
    }

    private void DrawSharedInfrastructure(DrawingContext context, IReadOnlySet<string> focused)
    {
        if (!TryService("conveyance", out var conveyance, out var center))
            return;

        using (context.PushOpacity(ContextOpacity(conveyance, focused)))
        {
            DrawIndustrialGround(context, center, Conveyance);
            DrawWarehouse(context, center + new Vector(-82, -8), 84, 34, Conveyance, conveyance.IsAvailable);
            DrawWarehouse(context, center + new Vector(20, 8), 72, 31, Conveyance, conveyance.IsAvailable);
            DrawSilo(context, center + new Vector(83, 22), 15, 42, Conveyance, conveyance.IsAvailable);
            DrawSilo(context, center + new Vector(118, 29), 13, 36, Conveyance, conveyance.IsAvailable);
            DrawRelayMast(context, center + new Vector(38, -25), Conveyance, conveyance.IsAvailable);
            DrawPlaceLabel(context, "Conveyance", center + new Vector(0, 104), Conveyance, conveyance.IsAvailable);
        }

        DrawConsumerFacility(context, conveyance, center);
    }

    private void DrawConsumerFacility(DrawingContext context, AtlasNodePresentationViewModel conveyance, Point conveyanceCenter)
    {
        var consumption = connections.FirstOrDefault(connection =>
            connection.Kind == AtlasConnectionKind.CapabilityDependency
            && connection.IsEnabled
            && string.Equals(connection.Source.ServiceIdentity?.Value, "vocation", StringComparison.Ordinal)
            && string.Equals(connection.Target.CapabilityIdentity?.Value, BuildAtlasProjectionUseCase.ConveyanceDurableDeliveryCapabilityId, StringComparison.Ordinal));
        if (consumption is null || !TryService("vocation", out var vocation, out var vocationCenter))
            return;

        var facility = vocationCenter + new Vector(128, 62);
        DrawIndustrialGround(context, facility, Conveyance, compact: true);
        DrawWarehouse(context, facility + new Vector(-20, 4), 43, 22, Conveyance, conveyance.IsAvailable && vocation.IsAvailable);
        DrawRelayMast(context, facility + new Vector(24, 2), Conveyance, conveyance.IsAvailable && vocation.IsAvailable);

        var route = new[] { facility + new Vector(26, 18), new Point(-115, 330), new Point(175, 355), conveyanceCenter + new Vector(-110, 5) };
        var geometry = WorldRoute(route);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Conveyance, 55)), Math.Max(5, 7 * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Conveyance, 165)), Math.Max(1, 1.4 * zoom)), geometry);

        if (zoom >= CloseDetailZoom)
            DrawSmallLabel(context, "device relay", facility + new Vector(0, 39), Conveyance);
    }

    private void DrawSemanticNetworks(DrawingContext context, IReadOnlySet<string> focused)
    {
        var geospatial = connections.FirstOrDefault(connection =>
            connection.Kind == AtlasConnectionKind.CapabilityDependency
            && connection.IsEnabled
            && string.Equals(connection.Source.ServiceIdentity?.Value, "vocation", StringComparison.Ordinal)
            && string.Equals(connection.Target.CapabilityIdentity?.Value, BuildAtlasProjectionUseCase.OrientationGeospatialCapabilityId, StringComparison.Ordinal));
        if (geospatial is null || !TryService("vocation", out _, out var vocationCenter) || !TryService("orientation", out _, out var orientationCenter))
            return;

        var focusedRoute = selectedNodeId is not null
            && (focused.Contains(geospatial.Source.NodeId) || focused.Contains(geospatial.Target.NodeId));
        if (zoom < CloseDetailZoom && !focusedRoute)
            return;

        var sourceFacility = vocationCenter + new Vector(84, -75);
        var targetFacility = orientationCenter + new Vector(-74, 48);
        var path = new[] { sourceFacility, new Point(-210, -22), new Point(45, -72), new Point(235, -105), targetFacility };
        var geometry = WorldRoute(path);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Orientation, focusedRoute ? (byte)76 : (byte)36)), Math.Max(4, 6 * zoom)), geometry);
        var pen = new Pen(new SolidColorBrush(WithAlpha(Orientation, focusedRoute ? (byte)220 : (byte)115)), Math.Max(0.9, 1.2 * zoom));
        context.DrawGeometry(null, pen, geometry);
        DrawGeoTerminal(context, sourceFacility, Orientation);
        DrawGeoTerminal(context, targetFacility, Orientation);

        if (focusedRoute && !reducedMotion)
            DrawMovingPulse(context, path, Orientation);
        if (zoom >= CloseDetailZoom)
            DrawSmallLabel(context, "geospatial link", sourceFacility + new Vector(15, 30), Orientation);
    }

    private void DrawCityAvenues(DrawingContext context, Point center)
    {
        DrawTownRoad(context, [center + new Vector(-185, -15), center + new Vector(-75, -10), center + new Vector(55, -5), center + new Vector(195, 8)], 12);
        DrawTownRoad(context, [center + new Vector(-155, 80), center + new Vector(-30, 75), center + new Vector(100, 80), center + new Vector(185, 88)], 9);
        DrawTownRoad(context, [center + new Vector(-22, -175), center + new Vector(-10, -65), center + new Vector(2, 65), center + new Vector(15, 150)], 10);
        DrawTownRoad(context, [center + new Vector(88, -155), center + new Vector(91, -55), center + new Vector(100, 55), center + new Vector(112, 145)], 8);
    }

    private void DrawTownRoad(DrawingContext context, Point[] points, double width = 6)
    {
        var geometry = WorldRoute(points);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Shadow, 160)), Math.Max(4, (width + 4) * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(Road), Math.Max(2, width * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(RoadEdge, 72)), Math.Max(0.5, 0.7 * zoom)), geometry);
    }

    private void DrawCityBlock(DrawingContext context, Point point, double width, double height, Color accent, int rows) =>
        DrawBoxBuilding(context, point, width, height, 14, accent, powered: true, rows);

    private void DrawCentralTower(DrawingContext context, Point point, Color accent)
    {
        DrawBoxBuilding(context, point, 62, 165, 19, accent, powered: true, 9);
        DrawBeacon(context, point + new Vector(0, -178), accent, true, 22);
    }

    private void DrawCivicBuilding(DrawingContext context, Point point, double width, double height, Color accent, bool powered)
    {
        DrawBoxBuilding(context, point, width, height, 11, accent, powered, 4);
        if (zoom >= MidDetailZoom)
        {
            var top = Screen(point + new Vector(0, -height - 10));
            context.DrawGeometry(new SolidColorBrush(WithAlpha(accent, 120)), null, Polygon([
                new(top.X - width * zoom * 0.42, top.Y + 10 * zoom),
                new(top.X, top.Y),
                new(top.X + width * zoom * 0.42, top.Y + 10 * zoom)
            ]));
        }
    }

    private void DrawPitchedBuilding(DrawingContext context, Point point, double width, double height, Color accent, bool powered)
    {
        DrawBoxBuilding(context, point, width, height, 9, accent, powered, 2);
        var basePoint = Screen(point);
        var roofY = basePoint.Y - height * zoom;
        context.DrawGeometry(
            new SolidColorBrush(WithAlpha(Mix(Color.Parse("#29362D"), accent, 0.18), 245)),
            new Pen(new SolidColorBrush(WithAlpha(accent, 74)), 0.7),
            Polygon([
                new(basePoint.X - width * zoom / 2, roofY),
                new(basePoint.X, roofY - 11 * zoom),
                new(basePoint.X + width * zoom / 2, roofY)
            ]));
    }

    private void DrawLongHall(DrawingContext context, Point point, double width, double height, Color accent, bool powered) =>
        DrawBoxBuilding(context, point, width, height, 8, accent, powered, 2);

    private void DrawLibraryWing(DrawingContext context, Point point, double width, double height, Color accent, bool powered)
    {
        DrawBoxBuilding(context, point, width, height, 8, accent, powered, 2);
        if (zoom < MidDetailZoom)
            return;
        var screen = Screen(point);
        context.DrawLine(new Pen(new SolidColorBrush(WithAlpha(Window, powered ? (byte)150 : (byte)50)), 1),
            new Point(screen.X - width * zoom * 0.38, screen.Y - height * zoom * 0.55),
            new Point(screen.X + width * zoom * 0.38, screen.Y - height * zoom * 0.55));
    }

    private void DrawLanternHall(DrawingContext context, Point point, Color accent, bool powered)
    {
        DrawBoxBuilding(context, point, 56, 78, 12, accent, powered, 5);
        var light = Screen(point + new Vector(0, -91));
        context.DrawGeometry(new SolidColorBrush(WithAlpha(accent, powered ? (byte)180 : (byte)65)), null, Polygon([
            new(light.X - 16 * zoom, light.Y + 14 * zoom), new(light.X, light.Y), new(light.X + 16 * zoom, light.Y + 14 * zoom)
        ]));
        if (powered)
            context.DrawEllipse(Radial(accent, 62), null, light, 23 * zoom, 18 * zoom);
    }

    private void DrawPavilion(DrawingContext context, Point point, Color accent, bool powered)
    {
        DrawBoxBuilding(context, point, 39, 24, 7, accent, powered, 1);
        var screen = Screen(point);
        context.DrawLine(new Pen(new SolidColorBrush(WithAlpha(accent, 90)), 0.8),
            new Point(screen.X - 22 * zoom, screen.Y - 27 * zoom), new Point(screen.X + 22 * zoom, screen.Y - 27 * zoom));
    }

    private void DrawSurveyPavilion(DrawingContext context, Point point, Color accent, bool powered)
    {
        DrawBoxBuilding(context, point, 82, 31, 9, accent, powered, 2);
        if (zoom >= MidDetailZoom)
        {
            var screen = Screen(point);
            context.DrawLine(new Pen(new SolidColorBrush(WithAlpha(accent, 120)), 1),
                new Point(screen.X - 35 * zoom, screen.Y - 38 * zoom), new Point(screen.X + 35 * zoom, screen.Y - 38 * zoom));
        }
    }

    private void DrawObservationTower(DrawingContext context, Point point, Color accent, bool powered)
    {
        DrawBoxBuilding(context, point, 40, 96, 10, accent, powered, 6);
        var deck = Screen(point + new Vector(0, -102));
        context.FillRectangle(new SolidColorBrush(WithAlpha(accent, 130)), new Rect(deck.X - 28 * zoom, deck.Y, 56 * zoom, 5 * zoom));
        DrawBeacon(context, point + new Vector(0, -116), accent, powered, 17);
    }

    private void DrawSurveyMast(DrawingContext context, Point point, Color accent, bool powered)
    {
        var screen = Screen(point);
        var pen = new Pen(new SolidColorBrush(WithAlpha(accent, powered ? (byte)190 : (byte)75)), Math.Max(0.8, 1 * zoom));
        context.DrawLine(pen, screen, new Point(screen.X, screen.Y - 62 * zoom));
        context.DrawLine(pen, new Point(screen.X - 14 * zoom, screen.Y), new Point(screen.X, screen.Y - 62 * zoom));
        context.DrawLine(pen, new Point(screen.X + 14 * zoom, screen.Y), new Point(screen.X, screen.Y - 62 * zoom));
        if (powered)
            context.DrawEllipse(Radial(accent, 65), null, new Point(screen.X, screen.Y - 64 * zoom), 18 * zoom, 18 * zoom);
    }

    private void DrawWarehouse(DrawingContext context, Point point, double width, double height, Color accent, bool powered)
    {
        DrawBoxBuilding(context, point, width, height, 8, accent, powered, 1);
        if (zoom < MidDetailZoom)
            return;
        var screen = Screen(point);
        context.FillRectangle(new SolidColorBrush(WithAlpha(Shadow, 205)), new Rect(
            screen.X - width * zoom * 0.15, screen.Y - height * zoom * 0.42,
            width * zoom * 0.3, height * zoom * 0.42));
    }

    private void DrawBoxBuilding(DrawingContext context, Point baseWorld, double width, double height, double depth, Color accent, bool powered, int rows)
    {
        var p = Screen(baseWorld);
        var w = width * zoom;
        var h = height * zoom;
        var d = depth * zoom;
        var left = p.X - w / 2;
        var right = p.X + w / 2;
        var top = p.Y - h;

        context.DrawGeometry(new SolidColorBrush(WithAlpha(Shadow, 120)), null, Polygon([
            new(left + 7 * zoom, p.Y + 6 * zoom), new(right + 15 * zoom, p.Y + 6 * zoom),
            new(right + 24 * zoom, p.Y + 13 * zoom), new(left + 15 * zoom, p.Y + 13 * zoom)
        ]));

        var body = Mix(Color.Parse("#173127"), accent, 0.14);
        context.DrawGeometry(new SolidColorBrush(body), new Pen(new SolidColorBrush(WithAlpha(accent, 65)), 0.7),
            Polygon([new(left, top), new(right, top), new(right, p.Y), new(left, p.Y)]));
        context.DrawGeometry(new SolidColorBrush(Mix(body, Shadow, 0.30)), null, Polygon([
            new(right, top), new(right + d * 0.65, top - d * 0.45),
            new(right + d * 0.65, p.Y - d * 0.45), new(right, p.Y)
        ]));
        context.DrawGeometry(new SolidColorBrush(Mix(body, accent, 0.22)), null, Polygon([
            new(left, top), new(left + d * 0.65, top - d * 0.45),
            new(right + d * 0.65, top - d * 0.45), new(right, top)
        ]));

        if (zoom < MidDetailZoom)
            return;
        var rowCount = Math.Clamp(rows, 1, 9);
        var colCount = width >= 58 ? 3 : 2;
        for (var row = 0; row < rowCount; row++)
        {
            var y = top + (row + 1) * h / (rowCount + 1);
            for (var col = 0; col < colCount; col++)
            {
                var x = left + (col + 1) * w / (colCount + 1);
                context.FillRectangle(new SolidColorBrush(WithAlpha(powered ? Window : Muted, powered ? (byte)200 : (byte)55)),
                    new Rect(x - 1.8 * zoom, y - 1.2 * zoom, 3.6 * zoom, 2.4 * zoom));
            }
        }
    }

    private void DrawSilo(DrawingContext context, Point point, double radius, double height, Color accent, bool powered)
    {
        var p = Screen(point);
        var r = radius * zoom;
        var h = height * zoom;
        context.FillRectangle(new SolidColorBrush(Mix(Color.Parse("#252A29"), accent, 0.10)), new Rect(p.X - r, p.Y - h, r * 2, h));
        context.DrawEllipse(new SolidColorBrush(Mix(Color.Parse("#363D3A"), accent, 0.16)), null, new Point(p.X, p.Y - h), r, r * 0.35);
        if (powered)
            context.DrawEllipse(new SolidColorBrush(Window), null, new Point(p.X + r * 0.45, p.Y - h * 0.65), 1.4 * zoom, 1.4 * zoom);
    }

    private void DrawRelayMast(DrawingContext context, Point point, Color accent, bool powered)
    {
        var p = Screen(point);
        var pen = new Pen(new SolidColorBrush(WithAlpha(accent, powered ? (byte)200 : (byte)70)), Math.Max(0.8, 1 * zoom));
        context.DrawLine(pen, p, new Point(p.X, p.Y - 60 * zoom));
        context.DrawLine(pen, p, new Point(p.X - 15 * zoom, p.Y - 5 * zoom));
        context.DrawLine(pen, p, new Point(p.X + 15 * zoom, p.Y - 5 * zoom));
        if (powered)
            DrawBeacon(context, point + new Vector(0, -62), accent, true, 18);
    }

    private void DrawBeacon(DrawingContext context, Point point, Color accent, bool powered, double radius)
    {
        if (!powered)
            return;
        var p = Screen(point);
        context.DrawEllipse(Radial(accent, 58), null, p, radius * zoom, radius * zoom);
        context.DrawEllipse(new SolidColorBrush(WithAlpha(accent, 240)), null, p, 1.8 * zoom, 1.8 * zoom);
    }

    private void DrawCampusCourt(DrawingContext context, Point center, Color accent)
    {
        var p = Screen(center + new Vector(0, 28));
        context.DrawEllipse(new SolidColorBrush(WithAlpha(Color.Parse("#6A6048"), 65)), new Pen(new SolidColorBrush(WithAlpha(accent, 55)), 0.7), p, 57 * zoom, 33 * zoom);
    }

    private void DrawSurveyPlaza(DrawingContext context, Point center, Color accent)
    {
        var geometry = WorldGeometry([
            center + new Vector(-70, 35), center + new Vector(-20, 12), center + new Vector(65, 25),
            center + new Vector(82, 70), center + new Vector(-35, 88)
        ], smooth: false, filled: true);
        context.DrawGeometry(new SolidColorBrush(WithAlpha(Color.Parse("#233338"), 150)), new Pen(new SolidColorBrush(WithAlpha(accent, 60)), 0.7), geometry);
    }

    private void DrawMarketCanopy(DrawingContext context, Point point, Color accent)
    {
        var p = Screen(point);
        context.DrawGeometry(new SolidColorBrush(WithAlpha(accent, 75)), null, Polygon([
            new(p.X - 25 * zoom, p.Y), new(p.X - 17 * zoom, p.Y - 13 * zoom),
            new(p.X + 22 * zoom, p.Y - 13 * zoom), new(p.X + 28 * zoom, p.Y)
        ]));
    }

    private void DrawArcade(DrawingContext context, Point point, Color accent)
    {
        var p = Screen(point);
        var pen = new Pen(new SolidColorBrush(WithAlpha(accent, 95)), Math.Max(0.6, 0.8 * zoom));
        for (var index = -3; index <= 3; index++)
        {
            var x = p.X + index * 13 * zoom;
            context.DrawLine(pen, new Point(x, p.Y), new Point(x, p.Y - 17 * zoom));
        }
        context.DrawLine(pen, new Point(p.X - 45 * zoom, p.Y - 17 * zoom), new Point(p.X + 45 * zoom, p.Y - 17 * zoom));
    }

    private void DrawBearingRose(DrawingContext context, Point point, Color accent)
    {
        var p = Screen(point);
        var pen = new Pen(new SolidColorBrush(WithAlpha(accent, 95)), Math.Max(0.6, 0.8 * zoom));
        context.DrawEllipse(null, pen, p, 21 * zoom, 21 * zoom);
        context.DrawLine(pen, new Point(p.X, p.Y - 29 * zoom), new Point(p.X, p.Y + 29 * zoom));
        context.DrawLine(pen, new Point(p.X - 29 * zoom, p.Y), new Point(p.X + 29 * zoom, p.Y));
    }

    private void DrawIndustrialGround(DrawingContext context, Point center, Color accent, bool compact = false)
    {
        var sx = compact ? 72d : 165d;
        var sy = compact ? 47d : 105d;
        var p = Screen(center);
        context.DrawGeometry(new SolidColorBrush(WithAlpha(Color.Parse("#171820"), 230)),
            new Pen(new SolidColorBrush(WithAlpha(accent, 62)), Math.Max(0.7, 0.9 * zoom)),
            Polygon([
                new(p.X - sx * zoom, p.Y - sy * zoom), new(p.X + sx * 0.82 * zoom, p.Y - sy * 0.86 * zoom),
                new(p.X + sx * zoom, p.Y + sy * 0.35 * zoom), new(p.X + sx * 0.45 * zoom, p.Y + sy * 0.80 * zoom),
                new(p.X - sx * 0.85 * zoom, p.Y + sy * 0.65 * zoom)
            ]));
    }

    private void DrawGeoTerminal(DrawingContext context, Point point, Color accent)
    {
        var p = Screen(point);
        context.DrawEllipse(new SolidColorBrush(WithAlpha(Color.Parse("#0C2027"), 245)), new Pen(new SolidColorBrush(WithAlpha(accent, 165)), 0.9), p, 10 * zoom, 10 * zoom);
        context.DrawLine(new Pen(new SolidColorBrush(WithAlpha(accent, 170)), 0.8), new Point(p.X, p.Y - 16 * zoom), new Point(p.X, p.Y + 16 * zoom));
    }

    private void DrawLocalLight(DrawingContext context, Point center, Color accent, bool available, double rx, double ry)
    {
        var p = Screen(center);
        var alpha = available ? (byte)42 : (byte)16;
        context.DrawEllipse(Radial(accent, alpha), null, p, rx * zoom, ry * zoom);
    }

    private void DrawTree(DrawingContext context, Point point, double size)
    {
        if (zoom < 0.62)
            return;
        var p = Screen(point);
        context.DrawLine(new Pen(new SolidColorBrush(Color.Parse("#65523B")), Math.Max(0.7, 0.9 * zoom)), p, new Point(p.X, p.Y + size * 1.2 * zoom));
        context.DrawEllipse(new SolidColorBrush(WithAlpha(Shadow, 75)), null, new Point(p.X + 3 * zoom, p.Y + 5 * zoom), size * 0.9 * zoom, size * 0.5 * zoom);
        context.DrawEllipse(new SolidColorBrush(Color.Parse("#1B4A32")), null, new Point(p.X - 2 * zoom, p.Y), size * 0.82 * zoom, size * 0.95 * zoom);
        context.DrawEllipse(new SolidColorBrush(Color.Parse("#2E7650")), null, new Point(p.X + 3 * zoom, p.Y - 3 * zoom), size * 0.55 * zoom, size * 0.65 * zoom);
    }

    private void DrawPlaceLabel(DrawingContext context, string title, Point point, Color accent, bool available, bool isCore = false)
    {
        var p = Screen(point);
        DrawCenteredText(context, title, p, Math.Clamp((isCore ? 13.2 : 11.2) * zoom, 9, isCore ? 15 : 13), Text);
        if (zoom >= MidDetailZoom)
        {
            var status = available ? "connected" : "unavailable";
            DrawCenteredText(context, status, new Point(p.X, p.Y + 16 * zoom), Math.Clamp(7 * zoom, 6.5, 8.5), WithAlpha(accent, available ? (byte)170 : (byte)90));
        }
    }

    private void DrawSmallLabel(DrawingContext context, string text, Point point, Color accent) =>
        DrawCenteredText(context, text, Screen(point), Math.Clamp(7.3 * zoom, 6.5, 9), WithAlpha(accent, 175));

    private void DrawContour(DrawingContext context, Point[] line, byte alpha) =>
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(LandEdge, alpha)), Math.Max(0.5, 0.7 * zoom)), WorldRoute(line));

    private void DrawMovingPulse(DrawingContext context, Point[] line, Color accent)
    {
        var phase = DateTime.UtcNow.TimeOfDay.TotalSeconds * 0.08 % 1;
        for (var index = 0; index < 2; index++)
        {
            var p = Screen(SamplePolyline(line, (phase + index * 0.5) % 1));
            context.DrawEllipse(Radial(accent, 115), null, p, 7, 7);
            context.DrawEllipse(new SolidColorBrush(WithAlpha(accent, 230)), null, p, 1.4, 1.4);
        }
    }

    private void DrawAtmosphere(DrawingContext context)
    {
        if (reducedMotion)
            return;
        var phase = DateTime.UtcNow.TimeOfDay.TotalSeconds * 0.015;
        for (var index = 0; index < 7; index++)
        {
            var x = (index * 197 + phase * 27) % Math.Max(1, Bounds.Width + 80) - 40;
            var y = (index * 113 + Math.Sin(phase + index) * 22 + Bounds.Height * 0.15) % Math.Max(1, Bounds.Height);
            context.DrawEllipse(new SolidColorBrush(Color.FromArgb(22, 108, 210, 170)), null, new Point(x, y), 1, 1);
        }
    }

    private void DrawVignette(DrawingContext context)
    {
        var brush = new RadialGradientBrush
        {
            Center = new RelativePoint(0.50, 0.47, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0.47, 0.43, RelativeUnit.Relative)
        };
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.70));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(88, 0, 2, 1), 1));
        context.FillRectangle(brush, new Rect(Bounds.Size));
    }

    private bool TryService(string serviceId, out AtlasNodePresentationViewModel service, out Point position)
    {
        service = nodes.FirstOrDefault(node => node.IsService && string.Equals(node.ServiceIdentity?.Value, serviceId, StringComparison.Ordinal))!;
        if (service is null || !positions.TryGetValue(service.NodeId, out position))
        {
            position = default;
            return false;
        }
        return true;
    }

    private static bool IsKnownProduct(AtlasNodePresentationViewModel node) =>
        node.ServiceIdentity?.Value is "vocation" or "illumination" or "orientation" or "conveyance";

    private double ContextOpacity(AtlasNodePresentationViewModel node, IReadOnlySet<string> focused)
    {
        if (selectedNodeId is null || string.Equals(node.NodeId, selectedNodeId, StringComparison.Ordinal) || focused.Contains(node.NodeId))
            return 1;
        return 0.40;
    }

    private AtlasNodePresentationViewModel? HitTestProduct(Point screenPoint)
    {
        foreach (var node in nodes.Where(node => node.IsCore || node.IsService).OrderByDescending(node => node.IsCore))
        {
            if (!positions.TryGetValue(node.NodeId, out var world))
                continue;
            var center = Screen(world);
            var radius = node.IsCore ? Math.Max(70, 180 * zoom) : node.IsSharedCapabilityProvider ? Math.Max(48, 115 * zoom) : Math.Max(50, 120 * zoom);
            var delta = screenPoint - center;
            if (delta.X * delta.X + delta.Y * delta.Y <= radius * radius)
                return node;
        }
        return null;
    }

    private StreamGeometry WorldGeometry(Point[] worldPoints, bool smooth, bool filled)
    {
        var points = worldPoints.Select(Screen).ToArray();
        var geometry = new StreamGeometry();
        using var gc = geometry.Open();
        if (points.Length == 0)
            return geometry;

        if (!smooth || points.Length < 3)
        {
            gc.BeginFigure(points[0], filled);
            for (var index = 1; index < points.Length; index++)
                gc.LineTo(points[index], true);
            gc.EndFigure(true);
            return geometry;
        }

        gc.BeginFigure(Mid(points[^1], points[0]), filled);
        for (var index = 0; index < points.Length; index++)
        {
            var current = points[index];
            var next = points[(index + 1) % points.Length];
            gc.QuadraticBezierTo(current, Mid(current, next), true);
        }
        gc.EndFigure(true);
        return geometry;
    }

    private StreamGeometry WorldRoute(Point[] worldPoints)
    {
        var points = worldPoints.Select(Screen).ToArray();
        var geometry = new StreamGeometry();
        using var gc = geometry.Open();
        if (points.Length == 0)
            return geometry;
        gc.BeginFigure(points[0], false);
        if (points.Length == 1)
        {
            gc.EndFigure(false);
            return geometry;
        }
        for (var index = 1; index < points.Length - 1; index++)
            gc.QuadraticBezierTo(points[index], Mid(points[index], points[index + 1]), true);
        gc.LineTo(points[^1], true);
        gc.EndFigure(false);
        return geometry;
    }

    private Point Screen(Point world) => new(
        (WorldCenterX + world.X) * zoom + translateX,
        (WorldCenterY + world.Y) * zoom + translateY);

    private static Point SamplePolyline(Point[] points, double t)
    {
        if (points.Length == 0)
            return default;
        if (points.Length == 1)
            return points[0];
        var scaled = Math.Clamp(t, 0, 1) * (points.Length - 1);
        var index = Math.Min(points.Length - 2, (int)Math.Floor(scaled));
        var local = scaled - index;
        return new Point(
            points[index].X + (points[index + 1].X - points[index].X) * local,
            points[index].Y + (points[index + 1].Y - points[index].Y) * local);
    }

    private static Vector Normalize(Vector vector)
    {
        var length = Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
        return length < 0.001 ? new Vector(1, 0) : new Vector(vector.X / length, vector.Y / length);
    }

    private static Point Mid(Point a, Point b) => new((a.X + b.X) / 2, (a.Y + b.Y) / 2);

    private static StreamGeometry Polygon(Point[] points)
    {
        var geometry = new StreamGeometry();
        using var gc = geometry.Open();
        if (points.Length == 0)
            return geometry;
        gc.BeginFigure(points[0], true);
        for (var index = 1; index < points.Length; index++)
            gc.LineTo(points[index], true);
        gc.EndFigure(true);
        return geometry;
    }

    private static RadialGradientBrush Radial(Color accent, byte alpha)
    {
        var brush = new RadialGradientBrush
        {
            Center = RelativePoint.Center,
            GradientOrigin = new RelativePoint(0.43, 0.36, RelativeUnit.Relative)
        };
        brush.GradientStops.Add(new GradientStop(WithAlpha(accent, alpha), 0));
        brush.GradientStops.Add(new GradientStop(WithAlpha(accent, (byte)Math.Max(0, alpha / 4)), 0.52));
        brush.GradientStops.Add(new GradientStop(WithAlpha(accent, 0), 1));
        return brush;
    }

    private static Color Mix(Color from, Color to, double t) => Color.FromArgb(
        (byte)Math.Clamp(Math.Round(from.A + (to.A - from.A) * t), 0, 255),
        (byte)Math.Clamp(Math.Round(from.R + (to.R - from.R) * t), 0, 255),
        (byte)Math.Clamp(Math.Round(from.G + (to.G - from.G) * t), 0, 255),
        (byte)Math.Clamp(Math.Round(from.B + (to.B - from.B) * t), 0, 255));

    private static Color WithAlpha(Color color, byte alpha) => Color.FromArgb(alpha, color.R, color.G, color.B);

    private static int StableHash(string value)
    {
        var hash = 17;
        foreach (var character in value)
            hash = unchecked(hash * 31 + character);
        return Math.Abs(hash == int.MinValue ? int.MaxValue : hash);
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

    private void RequestSceneFrame()
    {
        if (reducedMotion || selectedNodeId is null)
            return;
        TopLevel.GetTopLevel(this)?.RequestAnimationFrame(_ => InvalidateVisual());
    }
}
