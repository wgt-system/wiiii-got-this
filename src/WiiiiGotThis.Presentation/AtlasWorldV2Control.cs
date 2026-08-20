using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

/// <summary>
/// Authored World projection for Atlas.
/// First-class products are places inside one continuous landscape; shared capabilities are
/// facilities and infrastructure. This renderer owns presentation only.
/// </summary>
public sealed class AtlasWorldV2Control : Control
{
    private const double WorldCenterX = 1000d;
    private const double WorldCenterY = 660d;
    private const double MidZoom = 0.76d;
    private const double CloseZoom = 1.06d;

    private static readonly Dictionary<string, Point> ServicePlaces = new(StringComparer.Ordinal)
    {
        ["vocation"] = new(-500, 150),
        ["illumination"] = new(-270, -330),
        ["orientation"] = new(455, -175),
        ["conveyance"] = new(500, 315)
    };

    // Authored reserve sites keep direct products viable into the low teens without forcing groups.
    private static readonly Point[] ExpansionPlaces =
    [
        new(-695, -120), new(695, 55), new(-635, 420), new(95, -500),
        new(705, -390), new(-120, 475), new(-790, 205), new(790, 330),
        new(-475, -485), new(365, 505), new(35, 550), new(805, -70)
    ];

    private static readonly Point[] LandOutline =
    [
        new(-880, -420), new(-720, -555), new(-475, -585), new(-245, -550),
        new(-35, -605), new(225, -565), new(505, -505), new(705, -380),
        new(840, -190), new(865, 40), new(810, 250), new(715, 465),
        new(520, 560), new(265, 585), new(55, 555), new(-185, 585),
        new(-420, 560), new(-655, 470), new(-810, 315), new(-885, 95),
        new(-845, -130), new(-900, -275)
    ];

    private static readonly Color BackgroundTop = Color.Parse("#07140F");
    private static readonly Color BackgroundBottom = Color.Parse("#020806");
    private static readonly Color Land = Color.Parse("#173326");
    private static readonly Color LandDeep = Color.Parse("#0A1B13");
    private static readonly Color LandHighlight = Color.Parse("#244737");
    private static readonly Color Coast = Color.Parse("#4D765D");
    private static readonly Color Road = Color.Parse("#3A4139");
    private static readonly Color RoadEdge = Color.Parse("#798474");
    private static readonly Color Water = Color.Parse("#0B3437");
    private static readonly Color WaterEdge = Color.Parse("#3E7777");
    private static readonly Color Rail = Color.Parse("#879087");
    private static readonly Color Text = Color.Parse("#EAF3ED");
    private static readonly Color Muted = Color.Parse("#88A092");
    private static readonly Color WarmLight = Color.Parse("#F0C16C");
    private static readonly Color Shadow = Color.Parse("#010302");
    private static readonly Color Core = Color.Parse("#63DAB0");
    private static readonly Color Vocation = Color.Parse("#55C98F");
    private static readonly Color Illumination = Color.Parse("#D9B55C");
    private static readonly Color Orientation = Color.Parse("#6FB7DD");
    private static readonly Color Conveyance = Color.Parse("#9B83C3");

    private IReadOnlyList<AtlasNodePresentationViewModel> nodes = Array.Empty<AtlasNodePresentationViewModel>();
    private IReadOnlyList<AtlasConnectionPresentationViewModel> connections = Array.Empty<AtlasConnectionPresentationViewModel>();
    private readonly Dictionary<string, Point> nodePlaces = new(StringComparer.Ordinal);
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
        RebuildPlaces();
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

    public bool TryGetWorldPosition(string nodeId, out Point worldPoint) =>
        nodePlaces.TryGetValue(nodeId, out worldPoint);

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (nodes.Count == 0)
            return;

        var focus = AtlasPresentationFocus.Build(connections, selectedNodeId);
        DrawBackground(context);
        DrawContiguousTerrain(context);
        DrawWater(context);
        DrawRegionalLandUse(context);
        DrawRoadNetwork(context);
        DrawRailNetwork(context);
        DrawVegetation(context);
        DrawExpansionProducts(context, focus);
        DrawVocation(context, focus);
        DrawIllumination(context, focus);
        DrawOrientation(context, focus);
        DrawWgtCity(context, focus);
        DrawConveyance(context, focus);
        DrawCapabilityInfrastructure(context, focus);
        DrawAtmosphere(context);
        DrawVignette(context);
        RequestSceneFrame();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        var node = HitTestPlace(e.GetPosition(this));
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

    private void RebuildPlaces()
    {
        nodePlaces.Clear();
        if (nodes.FirstOrDefault(node => node.IsCore) is { } core)
            nodePlaces[core.NodeId] = new Point(50, 35);

        var expansionIndex = 0;
        foreach (var service in nodes.Where(node => node.IsService).OrderBy(node => node.Title, StringComparer.OrdinalIgnoreCase))
        {
            var serviceId = service.ServiceIdentity?.Value;
            if (serviceId is not null && ServicePlaces.TryGetValue(serviceId, out var authored))
            {
                nodePlaces[service.NodeId] = authored;
                continue;
            }

            nodePlaces[service.NodeId] = ExpansionPlaces[Math.Min(expansionIndex, ExpansionPlaces.Length - 1)];
            expansionIndex++;
        }
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

    private void DrawContiguousTerrain(DrawingContext context)
    {
        var shadow = LandOutline.Select(point => point + new Vector(12, 18)).ToArray();
        context.DrawGeometry(new SolidColorBrush(WithAlpha(Shadow, 190)), null, ClosedWorldShape(shadow, smooth: true));
        context.DrawGeometry(
            new SolidColorBrush(LandDeep),
            new Pen(new SolidColorBrush(WithAlpha(Coast, 115)), Math.Max(0.9, 1.25 * zoom)),
            ClosedWorldShape(LandOutline, smooth: true));

        var inner = LandOutline.Select(point => new Point(point.X * 0.974 + 4, point.Y * 0.965 - 1)).ToArray();
        context.DrawGeometry(new SolidColorBrush(Land), null, ClosedWorldShape(inner, smooth: true));

        var upland = new[]
        {
            new Point(-835,-330), new(-655,-470), new(-420,-495), new(-230,-450),
            new(-295,-320), new(-560,-285), new(-770,-230)
        };
        context.DrawGeometry(new SolidColorBrush(WithAlpha(LandHighlight, 115)), null, ClosedWorldShape(upland, smooth: true));

        var eastShelf = new[]
        {
            new Point(270,-405), new(520,-440), new(735,-285), new(785,-70),
            new(650,-35), new(420,-95), new(265,-210)
        };
        context.DrawGeometry(new SolidColorBrush(WithAlpha(Color.Parse("#16352E"), 175)), null, ClosedWorldShape(eastShelf, smooth: true));

        if (zoom < MidZoom)
            return;

        DrawContour(context, [new(-770,-390), new(-570,-455), new(-355,-430), new(-190,-365)], 38);
        DrawContour(context, [new(325,-350), new(500,-385), new(690,-285), new(735,-155)], 34);
        DrawContour(context, [new(-720,405), new(-480,475), new(-200,455), new(100,490), new(370,465)], 26);
    }

    private void DrawWater(DrawingContext context)
    {
        var river = new[]
        {
            new Point(-875,-45), new(-700,-10), new(-520,-45), new(-335,-20),
            new(-185,55), new(5,115), new(210,125), new(390,92), new(575,120),
            new(735,195), new(845,275)
        };
        var route = OpenWorldRoute(river);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Shadow, 170)), Math.Max(17, 25 * zoom)), route);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(Water), Math.Max(12, 18 * zoom)), route);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(WaterEdge, 145)), Math.Max(1, 1.5 * zoom)), route);

        var tributary = OpenWorldRoute([
            new(-485,-555), new(-430,-475), new(-392,-390), new(-370,-300), new(-395,-215), new(-455,-120), new(-510,-50)
        ]);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Water, 210)), Math.Max(3, 5.2 * zoom)), tributary);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(WaterEdge, 95)), Math.Max(0.7, 0.9 * zoom)), tributary);
    }

    private void DrawRegionalLandUse(DrawingContext context)
    {
        var vocationFields = new[]
        {
            new Point(-795,65), new(-635,30), new(-505,85), new(-470,280),
            new(-610,355), new(-800,300), new(-835,185)
        };
        context.DrawGeometry(new SolidColorBrush(Color.Parse("#1A3A25")), null, ClosedWorldShape(vocationFields, smooth: true));

        var campus = new[]
        {
            new Point(-465,-455), new(-305,-515), new(-115,-450), new(-95,-300),
            new(-205,-225), new(-390,-245), new(-485,-345)
        };
        context.DrawGeometry(new SolidColorBrush(Color.Parse("#2B3420")), null, ClosedWorldShape(campus, smooth: true));

        var orientationRidge = new[]
        {
            new Point(245,-355), new(455,-420), new(675,-310), new(700,-135),
            new(555,-60), new(335,-80), new(235,-205)
        };
        context.DrawGeometry(new SolidColorBrush(Color.Parse("#15343A")), null, ClosedWorldShape(orientationRidge, smooth: true));

        var industrialBelt = new[]
        {
            new Point(320,245), new(535,225), new(720,315), new(690,455),
            new(470,500), new(305,420)
        };
        context.DrawGeometry(new SolidColorBrush(Color.Parse("#23242B")), null, ClosedWorldShape(industrialBelt, smooth: false));

        if (zoom < MidZoom)
            return;

        var fieldPen = new Pen(new SolidColorBrush(WithAlpha(Color.Parse("#69804A"), 115)), Math.Max(0.5, 0.7 * zoom));
        for (var index = 0; index < 8; index++)
        {
            var y = 118 + index * 24;
            context.DrawLine(fieldPen, Screen(new Point(-775, y)), Screen(new Point(-545, y - 70)));
        }

        var campusPen = new Pen(new SolidColorBrush(WithAlpha(Illumination, 70)), Math.Max(0.7, 0.9 * zoom));
        context.DrawGeometry(null, campusPen, OpenWorldRoute([new(-420,-405), new(-350,-345), new(-270,-365), new(-185,-310)]));
        context.DrawGeometry(null, campusPen, OpenWorldRoute([new(-365,-470), new(-315,-410), new(-230,-425), new(-145,-360)]));

        for (var inset = 0; inset < 4; inset++)
        {
            var factor = 1 - inset * 0.11;
            var contour = orientationRidge
                .Select(point => new Point(470 + (point.X - 470) * factor, -220 + (point.Y + 220) * factor))
                .ToArray();
            context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Orientation, 34)), Math.Max(0.5, 0.65 * zoom)), ClosedWorldShape(contour, smooth: true));
        }
    }

    private void DrawRoadNetwork(DrawingContext context)
    {
        DrawRoad(context, [new(-580,145), new(-430,85), new(-250,75), new(-85,45), new(15,40)]);
        DrawRoad(context, [new(-295,-315), new(-245,-220), new(-145,-120), new(-15,-35), new(40,20)]);
        DrawRoad(context, [new(110,20), new(235,-15), new(345,-95), new(455,-165)]);
        DrawRoad(context, [new(125,80), new(265,160), new(385,240), new(500,315)]);
        DrawRoad(context, [new(-565,285), new(-330,365), new(-35,375), new(250,350), new(515,325)], secondary: true);
        DrawRoad(context, [new(-705,-165), new(-550,-105), new(-390,-125), new(-250,-210)], secondary: true);
    }

    private void DrawRoad(DrawingContext context, Point[] points, bool secondary = false)
    {
        var geometry = OpenWorldRoute(points);
        var outer = secondary ? 9d : 15d;
        var inner = secondary ? 5d : 9d;
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Shadow, 190)), Math.Max(5, outer * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(Road), Math.Max(3, inner * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(RoadEdge, secondary ? (byte)70 : (byte)120)), Math.Max(0.6, 0.9 * zoom)), geometry);
    }

    private void DrawRailNetwork(DrawingContext context)
    {
        var track = new[] { new Point(-795,410), new(-565,390), new(-270,420), new(35,405), new(315,360), new(515,315), new(770,360) };
        var geometry = OpenWorldRoute(track);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Shadow, 175)), Math.Max(4, 6 * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Rail, 215)), Math.Max(1.6, 2.2 * zoom)), geometry);

        if (zoom < MidZoom)
            return;

        var sleeperPen = new Pen(new SolidColorBrush(WithAlpha(Rail, 150)), Math.Max(0.6, 0.8 * zoom));
        for (var index = 0; index < 18; index++)
        {
            var t = index / 17d;
            var point = SamplePolyline(track, t);
            var next = SamplePolyline(track, Math.Min(1, t + 0.018));
            var tangent = Normalize(next - point);
            var normal = new Vector(-tangent.Y, tangent.X);
            context.DrawLine(sleeperPen, Screen(point - normal * 5), Screen(point + normal * 5));
        }
    }

    private void DrawVegetation(DrawingContext context)
    {
        Point[] canopy =
        [
            new(-800,-330), new(-755,-370), new(-700,-395), new(-650,-430), new(-585,-455),
            new(-520,-485), new(-440,-505), new(-345,-510), new(-130,-535), new(-55,-520),
            new(45,-535), new(150,-500), new(235,-465), new(720,-65), new(760,10),
            new(735,95), new(785,175), new(695,485), new(615,515), new(515,535),
            new(390,540), new(255,535), new(100,520), new(-55,530), new(-220,535),
            new(-385,520), new(-565,475), new(-700,430), new(-790,355)
        ];
        foreach (var tree in canopy)
            DrawTree(context, tree, 9);

        if (zoom < MidZoom)
            return;

        Point[] local =
        [
            new(-610,75), new(-570,55), new(-530,70), new(-455,260),
            new(-345,-255), new(-305,-245), new(-230,-250), new(-175,-275),
            new(190,95), new(225,112), new(265,92), new(305,105)
        ];
        foreach (var tree in local)
            DrawTree(context, tree, 6.5);
    }

    private void DrawVocation(DrawingContext context, IReadOnlySet<string> focus)
    {
        if (!TryService("vocation", out var service, out var center))
            return;

        using (context.PushOpacity(ContextOpacity(service, focus)))
        {
            DrawLocalRoad(context, [center + new Vector(-135,15), center + new Vector(-30,-4), center + new Vector(118,18)], 7);
            DrawLocalRoad(context, [center + new Vector(-35,-105), center + new Vector(-25,-8), center + new Vector(5,105)], 6);
            DrawPitchedBuilding(context, center + new Vector(-105,-32), 64, 38, Vocation, service.IsAvailable);
            DrawPitchedBuilding(context, center + new Vector(78,-28), 58, 35, Vocation, service.IsAvailable);
            DrawCivicBuilding(context, center + new Vector(-18,-48), 58, 72, Vocation, service.IsAvailable);
            DrawLongBuilding(context, center + new Vector(85,58), 94, 34, Vocation, service.IsAvailable);
            DrawPitchedBuilding(context, center + new Vector(-86,67), 52, 30, Vocation, service.IsAvailable);
            DrawWaterTower(context, center + new Vector(145,-42), Vocation, service.IsAvailable);
            if (zoom >= CloseZoom)
                DrawMarketSquare(context, center + new Vector(-4,48), Vocation);
            DrawPlaceName(context, "Vocation", center + new Vector(0,132), Vocation, service.IsAvailable);
        }
    }

    private void DrawIllumination(DrawingContext context, IReadOnlySet<string> focus)
    {
        if (!TryService("illumination", out var service, out var center))
            return;

        using (context.PushOpacity(ContextOpacity(service, focus)))
        {
            DrawCampusCourt(context, center + new Vector(0,25), Illumination);
            DrawLongBuilding(context, center + new Vector(-92,10), 92, 32, Illumination, service.IsAvailable);
            DrawLongBuilding(context, center + new Vector(92,8), 88, 32, Illumination, service.IsAvailable);
            DrawCivicBuilding(context, center + new Vector(0,-55), 62, 76, Illumination, service.IsAvailable);
            DrawPavilion(context, center + new Vector(-68,78), Illumination, service.IsAvailable);
            DrawPavilion(context, center + new Vector(70,77), Illumination, service.IsAvailable);
            DrawPitchedBuilding(context, center + new Vector(0,78), 50, 29, Illumination, service.IsAvailable);
            if (zoom >= CloseZoom)
                DrawArcade(context, center + new Vector(0,38), Illumination);
            DrawPlaceName(context, "Illumination", center + new Vector(0,139), Illumination, service.IsAvailable);
        }
    }

    private void DrawOrientation(DrawingContext context, IReadOnlySet<string> focus)
    {
        if (!TryService("orientation", out var service, out var center))
            return;

        using (context.PushOpacity(ContextOpacity(service, focus)))
        {
            DrawSurveyTerrace(context, center + new Vector(-10,45), Orientation);
            DrawPavilion(context, center + new Vector(-95,28), Orientation, service.IsAvailable);
            DrawObservationTower(context, center + new Vector(15,-18), Orientation, service.IsAvailable);
            DrawPitchedBuilding(context, center + new Vector(86,56), 56, 30, Orientation, service.IsAvailable);
            DrawSurveyMast(context, center + new Vector(125,-25), Orientation, service.IsAvailable);
            DrawPavilion(context, center + new Vector(-12,82), Orientation, service.IsAvailable);
            if (zoom >= CloseZoom)
                DrawBearingRose(context, center + new Vector(-52,70), Orientation);
            DrawPlaceName(context, "Orientation", center + new Vector(0,140), Orientation, service.IsAvailable);
        }
    }

    private void DrawWgtCity(DrawingContext context, IReadOnlySet<string> focus)
    {
        var coreNode = nodes.FirstOrDefault(node => node.IsCore);
        if (coreNode is null || !nodePlaces.TryGetValue(coreNode.NodeId, out var center))
            return;

        using (context.PushOpacity(ContextOpacity(coreNode, focus)))
        {
            DrawUrbanGround(context, center);
            DrawLocalRoad(context, [center + new Vector(-225,-18), center + new Vector(-80,-10), center + new Vector(65,-5), center + new Vector(225,9)], 13);
            DrawLocalRoad(context, [center + new Vector(-205,85), center + new Vector(-50,78), center + new Vector(105,83), center + new Vector(215,92)], 10);
            DrawLocalRoad(context, [center + new Vector(-40,-205), center + new Vector(-25,-70), center + new Vector(-8,65), center + new Vector(5,178)], 10);
            DrawLocalRoad(context, [center + new Vector(98,-185), center + new Vector(102,-60), center + new Vector(110,65), center + new Vector(125,165)], 8);

            DrawCityBlock(context, center + new Vector(-160,-86), 66, 78, Core, 4);
            DrawCityBlock(context, center + new Vector(-76,-122), 68, 118, Core, 6);
            DrawCityBlock(context, center + new Vector(32,-144), 78, 160, Core, 8);
            DrawCityBlock(context, center + new Vector(138,-88), 66, 92, Core, 5);
            DrawCityBlock(context, center + new Vector(-166,34), 74, 64, Core, 3);
            DrawCityBlock(context, center + new Vector(-70,55), 78, 86, Core, 4);
            DrawCityBlock(context, center + new Vector(48,58), 84, 102, Core, 5);
            DrawCityBlock(context, center + new Vector(155,38), 72, 68, Core, 3);
            DrawCityBlock(context, center + new Vector(-118,135), 62, 54, Core, 3);
            DrawCityBlock(context, center + new Vector(74,142), 70, 58, Core, 3);
            DrawCentralTower(context, center + new Vector(28,-4), Core);
            DrawPlaceName(context, "WIIII GOT THIS", center + new Vector(0,205), Core, true, isCore: true);
        }
    }

    private void DrawConveyance(DrawingContext context, IReadOnlySet<string> focus)
    {
        if (!TryService("conveyance", out var service, out var center))
            return;

        using (context.PushOpacity(ContextOpacity(service, focus)))
        {
            DrawIndustrialGround(context, center, compact: false);
            DrawWarehouse(context, center + new Vector(-105,-6), 98, 38, Conveyance, service.IsAvailable);
            DrawWarehouse(context, center + new Vector(12,15), 82, 34, Conveyance, service.IsAvailable);
            DrawWarehouse(context, center + new Vector(-52,66), 70, 28, Conveyance, service.IsAvailable);
            DrawSilo(context, center + new Vector(105,27), 17, 48, Conveyance, service.IsAvailable);
            DrawSilo(context, center + new Vector(140,33), 14, 40, Conveyance, service.IsAvailable);
            DrawRelayMast(context, center + new Vector(42,-33), Conveyance, service.IsAvailable);
            if (zoom >= MidZoom)
                DrawSmallLabel(context, "Conveyance infrastructure", center + new Vector(4,116), Conveyance);
        }
    }

    private void DrawExpansionProducts(DrawingContext context, IReadOnlySet<string> focus)
    {
        foreach (var service in nodes.Where(node => node.IsPrimaryProductProvider && !IsKnownProduct(node)))
        {
            if (!nodePlaces.TryGetValue(service.NodeId, out var center))
                continue;

            using (context.PushOpacity(ContextOpacity(service, focus)))
            {
                DrawLocalRoad(context, [center + new Vector(-55,20), center, center + new Vector(62,13)], 5);
                DrawPitchedBuilding(context, center + new Vector(-38,0), 46, 30, Core, service.IsAvailable);
                DrawCivicBuilding(context, center + new Vector(30,-8), 46, 50, Core, service.IsAvailable);
                DrawTree(context, center + new Vector(-65,-28), 7);
                DrawTree(context, center + new Vector(62,-32), 7);
                DrawPlaceName(context, service.Title, center + new Vector(0,86), Core, service.IsAvailable);
            }
        }
    }

    private void DrawCapabilityInfrastructure(DrawingContext context, IReadOnlySet<string> focus)
    {
        DrawConveyanceConsumption(context);
        DrawGeospatialConsumption(context, focus);
    }

    private void DrawConveyanceConsumption(DrawingContext context)
    {
        var connection = connections.FirstOrDefault(connection =>
            connection.IsEnabled
            && connection.IsCapabilityUse
            && string.Equals(connection.Source.ServiceIdentity?.Value, "vocation", StringComparison.Ordinal)
            && string.Equals(connection.Target.CapabilityIdentity?.Value, BuildAtlasProjectionUseCase.ConveyanceDurableDeliveryCapabilityId, StringComparison.Ordinal));

        if (connection is null
            || !TryService("vocation", out var vocation, out var vocationCenter)
            || !TryService("conveyance", out var conveyance, out var conveyanceCenter))
        {
            return;
        }

        var facility = vocationCenter + new Vector(150,74);
        DrawIndustrialGround(context, facility, compact: true);
        DrawWarehouse(context, facility + new Vector(-18,4), 48, 23, Conveyance, vocation.IsAvailable && conveyance.IsAvailable);
        DrawRelayMast(context, facility + new Vector(30,4), Conveyance, vocation.IsAvailable && conveyance.IsAvailable);

        var route = new[]
        {
            facility + new Vector(34,20), new Point(-210,360), new(80,392), new(315,355), conveyanceCenter + new Vector(-120,15)
        };
        var geometry = OpenWorldRoute(route);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Conveyance, 120)), Math.Max(1.4, 1.9 * zoom)), geometry);

        if (!reducedMotion && string.Equals(selectedNodeId, vocation.NodeId, StringComparison.Ordinal))
            DrawMovingPulse(context, route, Conveyance);
    }

    private void DrawGeospatialConsumption(DrawingContext context, IReadOnlySet<string> focus)
    {
        var connection = connections.FirstOrDefault(connection =>
            connection.IsEnabled
            && connection.IsCapabilityUse
            && string.Equals(connection.Source.ServiceIdentity?.Value, "vocation", StringComparison.Ordinal)
            && string.Equals(connection.Target.CapabilityIdentity?.Value, BuildAtlasProjectionUseCase.OrientationGeospatialCapabilityId, StringComparison.Ordinal));

        if (connection is null
            || !TryService("vocation", out var vocation, out var vocationCenter)
            || !TryService("orientation", out var orientation, out var orientationCenter))
        {
            return;
        }

        var focused = focus.Contains(vocation.NodeId) || focus.Contains(orientation.NodeId);
        if (zoom < CloseZoom && !focused)
            return;

        var vocationTerminal = vocationCenter + new Vector(128,-68);
        var orientationTerminal = orientationCenter + new Vector(-125,62);
        DrawGeoTerminal(context, vocationTerminal, Orientation);
        DrawGeoTerminal(context, orientationTerminal, Orientation);

        var route = new[]
        {
            vocationTerminal, new Point(-150,-5), new(85,-30), new(250,-75), orientationTerminal
        };
        var geometry = OpenWorldRoute(route);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Orientation, 145)), Math.Max(1.1, 1.5 * zoom)), geometry);

        if (!reducedMotion && focused)
            DrawMovingPulse(context, route, Orientation);
    }

    private void DrawUrbanGround(DrawingContext context, Point center)
    {
        var p = Screen(center);
        var points = new[]
        {
            new Point(p.X - 255 * zoom, p.Y - 180 * zoom),
            new(p.X - 110 * zoom, p.Y - 225 * zoom),
            new(p.X + 105 * zoom, p.Y - 215 * zoom),
            new(p.X + 250 * zoom, p.Y - 125 * zoom),
            new(p.X + 245 * zoom, p.Y + 125 * zoom),
            new(p.X + 80 * zoom, p.Y + 205 * zoom),
            new(p.X - 145 * zoom, p.Y + 195 * zoom),
            new(p.X - 265 * zoom, p.Y + 75 * zoom)
        };
        context.DrawGeometry(new SolidColorBrush(Color.Parse("#153A2D")), new Pen(new SolidColorBrush(WithAlpha(Core, 55)), 0.8), Polygon(points));
    }

    private void DrawPitchedBuilding(DrawingContext context, Point point, double width, double height, Color accent, bool powered)
    {
        var p = Screen(point);
        var w = width * zoom;
        var h = height * zoom;
        var body = new Rect(p.X - w / 2, p.Y - h, w, h);
        context.FillRectangle(new SolidColorBrush(Color.Parse("#13271D")), body);
        context.DrawRectangle(new Pen(new SolidColorBrush(WithAlpha(accent, 95)), Math.Max(0.7, 0.9 * zoom)), body);
        context.DrawGeometry(new SolidColorBrush(Color.Parse("#26362A")), null, Polygon([
            new(p.X - w * 0.58, p.Y - h), new(p.X, p.Y - h - 16 * zoom), new(p.X + w * 0.58, p.Y - h)
        ]));
        if (powered)
            DrawWindows(context, body, accent, 3, 1);
    }

    private void DrawCivicBuilding(DrawingContext context, Point point, double width, double height, Color accent, bool powered)
    {
        var p = Screen(point);
        var w = width * zoom;
        var h = height * zoom;
        var body = new Rect(p.X - w / 2, p.Y - h, w, h);
        context.FillRectangle(new SolidColorBrush(Color.Parse("#173025")), body);
        context.DrawRectangle(new Pen(new SolidColorBrush(WithAlpha(accent, 120)), Math.Max(0.8, 1 * zoom)), body);
        context.DrawGeometry(new SolidColorBrush(Color.Parse("#2A3E31")), null, Polygon([
            new(p.X - w * 0.56, p.Y - h), new(p.X, p.Y - h - 14 * zoom), new(p.X + w * 0.56, p.Y - h)
        ]));
        if (powered)
            DrawWindows(context, body, accent, 3, 3);
    }

    private void DrawLongBuilding(DrawingContext context, Point point, double width, double height, Color accent, bool powered)
    {
        var p = Screen(point);
        var rect = new Rect(p.X - width * zoom / 2, p.Y - height * zoom, width * zoom, height * zoom);
        context.FillRectangle(new SolidColorBrush(Color.Parse("#14291F")), rect);
        context.DrawRectangle(new Pen(new SolidColorBrush(WithAlpha(accent, 100)), Math.Max(0.7, 0.9 * zoom)), rect);
        if (powered)
            DrawWindows(context, rect, accent, 5, 1);
    }

    private void DrawCityBlock(DrawingContext context, Point point, double width, double height, Color accent, int floors)
    {
        var p = Screen(point);
        var w = width * zoom;
        var h = height * zoom;
        var rect = new Rect(p.X - w / 2, p.Y - h, w, h);
        context.FillRectangle(new SolidColorBrush(Color.Parse("#143126")), rect);
        context.DrawRectangle(new Pen(new SolidColorBrush(WithAlpha(accent, 105)), Math.Max(0.7, 0.9 * zoom)), rect);
        context.DrawGeometry(new SolidColorBrush(Color.Parse("#204235")), null, Polygon([
            new(p.X - w / 2, p.Y - h), new(p.X - w / 2 + 10 * zoom, p.Y - h - 8 * zoom),
            new(p.X + w / 2 + 10 * zoom, p.Y - h - 8 * zoom), new(p.X + w / 2, p.Y - h)
        ]));
        DrawWindows(context, rect, accent, 3, Math.Max(2, Math.Min(6, floors)));
        context.FillRectangle(new SolidColorBrush(WithAlpha(Shadow, 95)), new Rect(rect.Right + 2 * zoom, rect.Top + 6 * zoom, 11 * zoom, rect.Height - 4 * zoom));
    }

    private void DrawCentralTower(DrawingContext context, Point point, Color accent)
    {
        var p = Screen(point);
        var rect = new Rect(p.X - 25 * zoom, p.Y - 190 * zoom, 50 * zoom, 190 * zoom);
        context.FillRectangle(new SolidColorBrush(Color.Parse("#173E30")), rect);
        context.DrawRectangle(new Pen(new SolidColorBrush(WithAlpha(accent, 160)), Math.Max(1, 1.2 * zoom)), rect);
        DrawWindows(context, rect, accent, 2, 8);
        context.DrawLine(new Pen(new SolidColorBrush(WithAlpha(accent, 190)), Math.Max(0.8, 1 * zoom)),
            new Point(p.X, rect.Top), new Point(p.X, rect.Top - 36 * zoom));
        DrawBeacon(context, point + new Vector(0,-228), accent, true, 10);
    }

    private void DrawWarehouse(DrawingContext context, Point point, double width, double height, Color accent, bool powered)
    {
        var p = Screen(point);
        var w = width * zoom;
        var h = height * zoom;
        var rect = new Rect(p.X - w / 2, p.Y - h, w, h);
        context.FillRectangle(new SolidColorBrush(Color.Parse("#1B1D22")), rect);
        context.DrawRectangle(new Pen(new SolidColorBrush(WithAlpha(accent, 90)), Math.Max(0.7, 0.9 * zoom)), rect);
        context.DrawGeometry(new SolidColorBrush(Color.Parse("#2B2D34")), null, Polygon([
            new(p.X - w / 2, rect.Top), new(p.X - w * 0.18, rect.Top - 10 * zoom),
            new(p.X + w * 0.18, rect.Top - 10 * zoom), new(p.X + w / 2, rect.Top)
        ]));
        if (powered)
            DrawWindows(context, rect, accent, 4, 1);
    }

    private void DrawSilo(DrawingContext context, Point point, double radius, double height, Color accent, bool powered)
    {
        var p = Screen(point);
        var r = radius * zoom;
        var h = height * zoom;
        var rect = new Rect(p.X - r, p.Y - h, r * 2, h);
        context.FillRectangle(new SolidColorBrush(Color.Parse("#25272D")), rect);
        context.DrawRectangle(new Pen(new SolidColorBrush(WithAlpha(accent, 90)), 0.8), rect);
        context.DrawEllipse(new SolidColorBrush(Color.Parse("#32343A")), new Pen(new SolidColorBrush(WithAlpha(accent, 90)), 0.8), new Point(p.X, p.Y - h), r, r * 0.35);
        if (powered)
            context.DrawEllipse(new SolidColorBrush(WarmLight), null, new Point(p.X + r * 0.45, p.Y - h * 0.65), 1.4 * zoom, 1.4 * zoom);
    }

    private void DrawIndustrialGround(DrawingContext context, Point center, bool compact)
    {
        var x = compact ? 78d : 185d;
        var y = compact ? 50d : 118d;
        var p = Screen(center);
        context.DrawGeometry(new SolidColorBrush(WithAlpha(Color.Parse("#1A1B21"), 235)),
            new Pen(new SolidColorBrush(WithAlpha(Conveyance, 55)), Math.Max(0.7, 0.9 * zoom)),
            Polygon([
                new(p.X - x * zoom, p.Y - y * zoom), new(p.X + x * 0.72 * zoom, p.Y - y * 0.94 * zoom),
                new(p.X + x * zoom, p.Y + y * 0.22 * zoom), new(p.X + x * 0.52 * zoom, p.Y + y * 0.86 * zoom),
                new(p.X - x * 0.86 * zoom, p.Y + y * 0.62 * zoom)
            ]));
    }

    private void DrawCampusCourt(DrawingContext context, Point center, Color accent)
    {
        var p = Screen(center);
        context.DrawGeometry(new SolidColorBrush(WithAlpha(Color.Parse("#7A6D47"), 65)),
            new Pen(new SolidColorBrush(WithAlpha(accent, 60)), 0.8),
            Polygon([
                new(p.X - 62 * zoom,p.Y - 20 * zoom), new(p.X + 42 * zoom,p.Y - 31 * zoom),
                new(p.X + 70 * zoom,p.Y + 18 * zoom), new(p.X - 42 * zoom,p.Y + 32 * zoom)
            ]));
    }

    private void DrawSurveyTerrace(DrawingContext context, Point center, Color accent)
    {
        var p = Screen(center);
        context.DrawGeometry(new SolidColorBrush(WithAlpha(Color.Parse("#29434A"), 135)),
            new Pen(new SolidColorBrush(WithAlpha(accent, 60)), 0.8),
            Polygon([
                new(p.X - 88 * zoom,p.Y - 22 * zoom), new(p.X - 20 * zoom,p.Y - 46 * zoom),
                new(p.X + 78 * zoom,p.Y - 24 * zoom), new(p.X + 94 * zoom,p.Y + 34 * zoom),
                new(p.X - 48 * zoom,p.Y + 48 * zoom)
            ]));
    }

    private void DrawPavilion(DrawingContext context, Point point, Color accent, bool powered)
    {
        var p = Screen(point);
        var body = new Rect(p.X - 23 * zoom, p.Y - 24 * zoom, 46 * zoom, 24 * zoom);
        context.FillRectangle(new SolidColorBrush(Color.Parse("#173029")), body);
        context.DrawGeometry(new SolidColorBrush(Color.Parse("#27453C")), null, Polygon([
            new(p.X - 29 * zoom,p.Y - 24 * zoom), new(p.X,p.Y - 38 * zoom), new(p.X + 29 * zoom,p.Y - 24 * zoom)
        ]));
        if (powered)
            context.DrawEllipse(new SolidColorBrush(WarmLight), null, new Point(p.X, p.Y - 12 * zoom), 1.7 * zoom, 1.7 * zoom);
    }

    private void DrawObservationTower(DrawingContext context, Point point, Color accent, bool powered)
    {
        var p = Screen(point);
        var baseRect = new Rect(p.X - 18 * zoom, p.Y - 82 * zoom, 36 * zoom, 82 * zoom);
        context.FillRectangle(new SolidColorBrush(Color.Parse("#16313A")), baseRect);
        context.DrawRectangle(new Pen(new SolidColorBrush(WithAlpha(accent, 130)), 0.9), baseRect);
        var crown = new Rect(p.X - 30 * zoom, p.Y - 104 * zoom, 60 * zoom, 23 * zoom);
        context.FillRectangle(new SolidColorBrush(Color.Parse("#1D3B45")), crown);
        context.DrawRectangle(new Pen(new SolidColorBrush(WithAlpha(accent, 145)), 0.9), crown);
        if (powered)
            DrawBeacon(context, point + new Vector(0,-112), accent, true, 12);
    }

    private void DrawSurveyMast(DrawingContext context, Point point, Color accent, bool powered)
    {
        var p = Screen(point);
        var pen = new Pen(new SolidColorBrush(WithAlpha(accent, powered ? (byte)200 : (byte)85)), Math.Max(0.8, 1 * zoom));
        context.DrawLine(pen, p, new Point(p.X, p.Y - 68 * zoom));
        context.DrawLine(pen, new Point(p.X - 17 * zoom,p.Y - 50 * zoom), new Point(p.X + 17 * zoom,p.Y - 50 * zoom));
        context.DrawLine(pen, new Point(p.X - 12 * zoom,p.Y - 30 * zoom), new Point(p.X + 12 * zoom,p.Y - 30 * zoom));
        DrawBeacon(context, point + new Vector(0,-70), accent, powered, 9);
    }

    private void DrawRelayMast(DrawingContext context, Point point, Color accent, bool powered)
    {
        var p = Screen(point);
        var pen = new Pen(new SolidColorBrush(WithAlpha(accent, powered ? (byte)190 : (byte)75)), Math.Max(0.8, 1 * zoom));
        context.DrawLine(pen, p, new Point(p.X, p.Y - 62 * zoom));
        context.DrawLine(pen, p, new Point(p.X - 16 * zoom, p.Y - 5 * zoom));
        context.DrawLine(pen, p, new Point(p.X + 16 * zoom, p.Y - 5 * zoom));
        DrawBeacon(context, point + new Vector(0,-64), accent, powered, 11);
    }

    private void DrawWaterTower(DrawingContext context, Point point, Color accent, bool powered)
    {
        var p = Screen(point);
        var pen = new Pen(new SolidColorBrush(WithAlpha(accent, 105)), Math.Max(0.7, 0.9 * zoom));
        context.DrawLine(pen, new Point(p.X - 10 * zoom,p.Y), new Point(p.X - 6 * zoom,p.Y - 48 * zoom));
        context.DrawLine(pen, new Point(p.X + 10 * zoom,p.Y), new Point(p.X + 6 * zoom,p.Y - 48 * zoom));
        context.DrawEllipse(new SolidColorBrush(Color.Parse("#263B30")), pen, new Point(p.X,p.Y - 53 * zoom), 18 * zoom, 9 * zoom);
        if (powered)
            context.DrawEllipse(new SolidColorBrush(WarmLight), null, new Point(p.X + 9 * zoom,p.Y - 54 * zoom), 1.3 * zoom, 1.3 * zoom);
    }

    private void DrawMarketSquare(DrawingContext context, Point point, Color accent)
    {
        var p = Screen(point);
        context.DrawGeometry(new SolidColorBrush(WithAlpha(accent, 48)), new Pen(new SolidColorBrush(WithAlpha(accent, 80)), 0.7), Polygon([
            new(p.X - 35 * zoom,p.Y - 12 * zoom), new(p.X + 25 * zoom,p.Y - 16 * zoom),
            new(p.X + 38 * zoom,p.Y + 12 * zoom), new(p.X - 25 * zoom,p.Y + 17 * zoom)
        ]));
    }

    private void DrawArcade(DrawingContext context, Point point, Color accent)
    {
        var p = Screen(point);
        var pen = new Pen(new SolidColorBrush(WithAlpha(accent, 100)), Math.Max(0.6, 0.8 * zoom));
        for (var index = -3; index <= 3; index++)
        {
            var x = p.X + index * 13 * zoom;
            context.DrawLine(pen, new Point(x,p.Y), new Point(x,p.Y - 20 * zoom));
        }
        context.DrawLine(pen, new Point(p.X - 47 * zoom,p.Y - 20 * zoom), new Point(p.X + 47 * zoom,p.Y - 20 * zoom));
    }

    private void DrawBearingRose(DrawingContext context, Point point, Color accent)
    {
        var p = Screen(point);
        var pen = new Pen(new SolidColorBrush(WithAlpha(accent, 105)), Math.Max(0.6, 0.8 * zoom));
        context.DrawEllipse(null, pen, p, 24 * zoom, 24 * zoom);
        context.DrawLine(pen, new Point(p.X,p.Y - 33 * zoom), new Point(p.X,p.Y + 33 * zoom));
        context.DrawLine(pen, new Point(p.X - 33 * zoom,p.Y), new Point(p.X + 33 * zoom,p.Y));
    }

    private void DrawGeoTerminal(DrawingContext context, Point point, Color accent)
    {
        var p = Screen(point);
        context.DrawEllipse(new SolidColorBrush(Color.Parse("#0D2229")), new Pen(new SolidColorBrush(WithAlpha(accent, 175)), 0.9), p, 10 * zoom, 10 * zoom);
        context.DrawLine(new Pen(new SolidColorBrush(WithAlpha(accent, 175)), 0.8),
            new Point(p.X,p.Y - 17 * zoom), new Point(p.X,p.Y + 17 * zoom));
    }

    private void DrawBeacon(DrawingContext context, Point point, Color accent, bool powered, double radius)
    {
        if (!powered)
            return;

        var p = Screen(point);
        context.DrawEllipse(Radial(accent, 64), null, p, radius * zoom, radius * zoom);
        context.DrawEllipse(new SolidColorBrush(WithAlpha(accent, 240)), null, p, 1.8 * zoom, 1.8 * zoom);
    }

    private void DrawWindows(DrawingContext context, Rect rect, Color accent, int columns, int rows)
    {
        if (zoom < 0.60)
            return;

        var widthStep = rect.Width / (columns + 1);
        var heightStep = rect.Height / (rows + 1);
        var lit = new SolidColorBrush(WithAlpha(WarmLight, 175));
        var cool = new SolidColorBrush(WithAlpha(accent, 100));
        for (var row = 1; row <= rows; row++)
        {
            for (var column = 1; column <= columns; column++)
            {
                var brush = (row + column) % 3 == 0 ? cool : lit;
                var x = rect.Left + column * widthStep;
                var y = rect.Top + row * heightStep;
                context.FillRectangle(brush, new Rect(x - 2 * zoom, y - 1.3 * zoom, 4 * zoom, 2.6 * zoom));
            }
        }
    }

    private void DrawTree(DrawingContext context, Point point, double size)
    {
        if (zoom < 0.60)
            return;

        var p = Screen(point);
        context.DrawLine(new Pen(new SolidColorBrush(Color.Parse("#65523B")), Math.Max(0.7, 0.9 * zoom)), p, new Point(p.X,p.Y + size * 1.15 * zoom));
        context.DrawEllipse(new SolidColorBrush(WithAlpha(Shadow, 82)), null, new Point(p.X + 3 * zoom,p.Y + 5 * zoom), size * 0.95 * zoom, size * 0.48 * zoom);
        context.DrawEllipse(new SolidColorBrush(Color.Parse("#1B4A32")), null, new Point(p.X - 2 * zoom,p.Y), size * 0.86 * zoom, size * 0.98 * zoom);
        context.DrawEllipse(new SolidColorBrush(Color.Parse("#327652")), null, new Point(p.X + 3 * zoom,p.Y - 3 * zoom), size * 0.56 * zoom, size * 0.65 * zoom);
    }

    private void DrawLocalRoad(DrawingContext context, Point[] points, double width)
    {
        var geometry = OpenWorldRoute(points);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Shadow, 175)), Math.Max(4, (width + 4) * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(Road), Math.Max(2, width * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(RoadEdge, 85)), Math.Max(0.5, 0.7 * zoom)), geometry);
    }

    private void DrawPlaceName(DrawingContext context, string title, Point point, Color accent, bool available, bool isCore = false)
    {
        var p = Screen(point);
        DrawCenteredText(context, title, p, Math.Clamp((isCore ? 13.7 : 11.5) * zoom, 9, isCore ? 16 : 13.4), Text);
        if (zoom >= MidZoom)
        {
            var stateColor = available ? accent : Muted;
            context.DrawEllipse(new SolidColorBrush(WithAlpha(stateColor, 195)), null, new Point(p.X,p.Y + 18 * zoom), 2.1 * zoom, 2.1 * zoom);
        }
    }

    private void DrawSmallLabel(DrawingContext context, string text, Point point, Color accent) =>
        DrawCenteredText(context, text, Screen(point), Math.Clamp(7.5 * zoom, 6.5, 9.2), WithAlpha(accent, 180));

    private void DrawContour(DrawingContext context, Point[] points, byte alpha) =>
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Coast, alpha)), Math.Max(0.5, 0.7 * zoom)), OpenWorldRoute(points));

    private void DrawMovingPulse(DrawingContext context, Point[] path, Color accent)
    {
        var phase = DateTime.UtcNow.TimeOfDay.TotalSeconds * 0.08 % 1;
        for (var index = 0; index < 2; index++)
        {
            var p = Screen(SamplePolyline(path, (phase + index * 0.5) % 1));
            context.DrawEllipse(Radial(accent, 110), null, p, 7, 7);
            context.DrawEllipse(new SolidColorBrush(WithAlpha(accent, 230)), null, p, 1.4, 1.4);
        }
    }

    private void DrawAtmosphere(DrawingContext context)
    {
        if (reducedMotion)
            return;

        var phase = DateTime.UtcNow.TimeOfDay.TotalSeconds * 0.014;
        for (var index = 0; index < 8; index++)
        {
            var x = (index * 211 + phase * 24) % Math.Max(1, Bounds.Width + 90) - 45;
            var y = (index * 127 + Math.Sin(phase + index) * 18 + Bounds.Height * 0.12) % Math.Max(1, Bounds.Height);
            context.DrawEllipse(new SolidColorBrush(Color.FromArgb(18, 112, 205, 170)), null, new Point(x,y), 1, 1);
        }
    }

    private void DrawVignette(DrawingContext context)
    {
        var brush = new RadialGradientBrush
        {
            Center = new RelativePoint(0.50, 0.47, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0.47, 0.43, RelativeUnit.Relative)
        };
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0,0,0,0), 0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0,0,0,0), 0.72));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(74,0,2,1), 1));
        context.FillRectangle(brush, new Rect(Bounds.Size));
    }

    private bool TryService(string serviceId, out AtlasNodePresentationViewModel service, out Point place)
    {
        service = nodes.FirstOrDefault(node => node.IsService && string.Equals(node.ServiceIdentity?.Value, serviceId, StringComparison.Ordinal))!;
        if (service is null || !nodePlaces.TryGetValue(service.NodeId, out place))
        {
            place = default;
            return false;
        }
        return true;
    }

    private static bool IsKnownProduct(AtlasNodePresentationViewModel node) =>
        node.ServiceIdentity?.Value is "vocation" or "illumination" or "orientation" or "conveyance";

    private double ContextOpacity(AtlasNodePresentationViewModel node, IReadOnlySet<string> focus)
    {
        if (selectedNodeId is null
            || string.Equals(node.NodeId, selectedNodeId, StringComparison.Ordinal)
            || focus.Contains(node.NodeId))
        {
            return 1;
        }
        return 0.44;
    }

    private AtlasNodePresentationViewModel? HitTestPlace(Point screenPoint)
    {
        foreach (var node in nodes.Where(node => node.IsCore || node.IsService).OrderByDescending(node => node.IsCore))
        {
            if (!nodePlaces.TryGetValue(node.NodeId, out var world))
                continue;

            var center = Screen(world);
            var radius = node.IsCore
                ? Math.Max(75, 200 * zoom)
                : node.IsSharedCapabilityProvider
                    ? Math.Max(50, 125 * zoom)
                    : Math.Max(52, 130 * zoom);
            var delta = screenPoint - center;
            if (delta.X * delta.X + delta.Y * delta.Y <= radius * radius)
                return node;
        }
        return null;
    }

    private StreamGeometry ClosedWorldShape(Point[] worldPoints, bool smooth)
    {
        var points = worldPoints.Select(Screen).ToArray();
        var geometry = new StreamGeometry();
        using var gc = geometry.Open();
        if (points.Length == 0)
            return geometry;

        if (!smooth || points.Length < 3)
        {
            gc.BeginFigure(points[0], true);
            for (var index = 1; index < points.Length; index++)
                gc.LineTo(points[index], true);
            gc.EndFigure(true);
            return geometry;
        }

        gc.BeginFigure(Mid(points[^1], points[0]), true);
        for (var index = 0; index < points.Length; index++)
        {
            var current = points[index];
            var next = points[(index + 1) % points.Length];
            gc.QuadraticBezierTo(current, Mid(current, next), true);
        }
        gc.EndFigure(true);
        return geometry;
    }

    private StreamGeometry OpenWorldRoute(Point[] worldPoints)
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
        return length < 0.001 ? new Vector(1,0) : new Vector(vector.X / length, vector.Y / length);
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
        brush.GradientStops.Add(new GradientStop(WithAlpha(accent, (byte)(alpha / 4)), 0.52));
        brush.GradientStops.Add(new GradientStop(WithAlpha(accent, 0), 1));
        return brush;
    }

    private static Color WithAlpha(Color color, byte alpha) => Color.FromArgb(alpha, color.R, color.G, color.B);

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
