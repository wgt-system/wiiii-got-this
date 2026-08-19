using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

/// <summary>
/// Authored World projection for Atlas.
///
/// The World deliberately represents product meaning rather than repository/process topology:
/// first-class products are places, shared capabilities are facilities/networks, and WGT is the
/// containing city. The control owns presentation only; provider/domain authority stays outside it.
/// </summary>
public sealed class AtlasWorldV2Control : Control
{
    private const double WorldCenterX = 1000d;
    private const double WorldCenterY = 660d;
    private const double MidZoom = 0.78d;
    private const double CloseZoom = 1.08d;

    private static readonly Dictionary<string, Point> ServicePlaces = new(StringComparer.Ordinal)
    {
        ["vocation"] = new(-475, 150),
        ["illumination"] = new(-250, -335),
        ["orientation"] = new(440, -155),
        ["conveyance"] = new(455, 305)
    };

    // Direct products intentionally remain viable into the low teens before semantic grouping.
    // These are curated expansion sites, not a radial/force layout.
    private static readonly Point[] ExpansionPlaces =
    [
        new(-675, -115), new(665, 95), new(-625, 395), new(95, -485),
        new(675, -365), new(-125, 445), new(-765, 190), new(765, 325),
        new(-470, -460), new(355, 480), new(35, 535), new(775, -65)
    ];

    private static readonly Point[] LandOutline =
    [
        new(-835, -480), new(-630, -570), new(-330, -555), new(-70, -585),
        new(235, -555), new(515, -495), new(735, -350), new(825, -115),
        new(790, 145), new(705, 425), new(500, 535), new(210, 570),
        new(-85, 545), new(-365, 585), new(-635, 500), new(-805, 300),
        new(-845, 35), new(-820, -235)
    ];

    private static readonly Color BackgroundTop = Color.Parse("#06130F");
    private static readonly Color BackgroundBottom = Color.Parse("#020806");
    private static readonly Color Land = Color.Parse("#10251B");
    private static readonly Color LandDeep = Color.Parse("#081710");
    private static readonly Color LandEdge = Color.Parse("#335B43");
    private static readonly Color Road = Color.Parse("#27382F");
    private static readonly Color RoadEdge = Color.Parse("#6C7F70");
    private static readonly Color Water = Color.Parse("#0A292B");
    private static readonly Color WaterEdge = Color.Parse("#2B6B6A");
    private static readonly Color Rail = Color.Parse("#68736C");
    private static readonly Color Text = Color.Parse("#E9F5EE");
    private static readonly Color Muted = Color.Parse("#89A394");
    private static readonly Color WarmLight = Color.Parse("#F3C672");
    private static readonly Color Shadow = Color.Parse("#010302");
    private static readonly Color Core = Color.Parse("#62E1B6");
    private static readonly Color Vocation = Color.Parse("#4FD39B");
    private static readonly Color Illumination = Color.Parse("#E3BB60");
    private static readonly Color Orientation = Color.Parse("#6DBCEB");
    private static readonly Color Conveyance = Color.Parse("#A88BD7");

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
        DrawVocation(context, focus);
        DrawIllumination(context, focus);
        DrawOrientation(context, focus);
        DrawExpansionProducts(context, focus);
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
            nodePlaces[core.NodeId] = new Point(55, 30);

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
        var shadowOutline = LandOutline.Select(point => point + new Vector(10, 13)).ToArray();
        context.DrawGeometry(new SolidColorBrush(WithAlpha(Shadow, 170)), null, ClosedWorldShape(shadowOutline, smooth: true));
        context.DrawGeometry(
            new SolidColorBrush(LandDeep),
            new Pen(new SolidColorBrush(WithAlpha(LandEdge, 85)), Math.Max(0.8, 1.1 * zoom)),
            ClosedWorldShape(LandOutline, smooth: true));

        var inner = LandOutline.Select(point => new Point(point.X * 0.968 + 3, point.Y * 0.958 - 2)).ToArray();
        context.DrawGeometry(new SolidColorBrush(WithAlpha(Land, 246)), null, ClosedWorldShape(inner, smooth: true));

        if (zoom >= MidZoom)
        {
            DrawContour(context, [new(-720, -405), new(-405, -465), new(-80, -450), new(250, -460), new(625, -325)], 28);
            DrawContour(context, [new(-745, 350), new(-430, 445), new(-80, 435), new(250, 465), new(610, 370)], 22);
            DrawContour(context, [new(-780, -135), new(-575, -185), new(-340, -165), new(-120, -215)], 15);
        }
    }

    private void DrawWater(DrawingContext context)
    {
        var river = new[]
        {
            new Point(-850, -65), new(-650, -20), new(-430, -52), new(-220, -5),
            new(-20, 95), new(215, 132), new(455, 100), new(675, 160), new(845, 250)
        };
        var route = OpenWorldRoute(river);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Shadow, 150)), Math.Max(16, 22 * zoom)), route);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(Water), Math.Max(11, 16 * zoom)), route);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(WaterEdge, 105)), Math.Max(1, 1.4 * zoom)), route);

        var tributary = OpenWorldRoute([
            new(-470, -535), new(-420, -465), new(-375, -390), new(-350, -300), new(-375, -210), new(-405, -130)
        ]);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(WaterEdge, 80)), Math.Max(2, 3.2 * zoom)), tributary);
    }

    private void DrawRegionalLandUse(DrawingContext context)
    {
        var vocationFields = new[]
        {
            new Point(-755, 105), new(-610, 42), new(-520, 100), new(-540, 285), new(-715, 335), new(-790, 250)
        };
        context.DrawGeometry(new SolidColorBrush(Color.Parse("#142D1D")), null, ClosedWorldShape(vocationFields, smooth: true));

        var illuminationGarden = new[]
        {
            new Point(-440, -455), new(-275, -510), new(-105, -410), new(-120, -270), new(-315, -235), new(-455, -325)
        };
        context.DrawGeometry(new SolidColorBrush(Color.Parse("#182A1B")), null, ClosedWorldShape(illuminationGarden, smooth: true));

        var orientationRidge = new[]
        {
            new Point(245, -315), new(455, -365), new(665, -250), new(635, -85), new(405, -45), new(255, -135)
        };
        context.DrawGeometry(new SolidColorBrush(Color.Parse("#10262A")), null, ClosedWorldShape(orientationRidge, smooth: true));

        var wgtGreenbelt = new[]
        {
            new Point(-140, -120), new(60, -145), new(235, -55), new(220, 130), new(80, 185), new(-135, 135)
        };
        context.DrawGeometry(new SolidColorBrush(Color.Parse("#143325")), null, ClosedWorldShape(wgtGreenbelt, smooth: true));

        if (zoom < MidZoom)
            return;

        var fieldPen = new Pen(new SolidColorBrush(Color.Parse("#35563B")), Math.Max(0.5, 0.7 * zoom));
        for (var index = 0; index < 7; index++)
        {
            var y = 135 + index * 23;
            context.DrawLine(fieldPen, Screen(new Point(-725, y)), Screen(new Point(-575, y - 48)));
        }

        var campusPen = new Pen(new SolidColorBrush(WithAlpha(Color.Parse("#8B7B56"), 95)), Math.Max(0.8, 1.2 * zoom));
        context.DrawGeometry(null, campusPen, OpenWorldRoute([new(-410, -395), new(-340, -335), new(-250, -350), new(-155, -300)]));
        context.DrawGeometry(null, campusPen, OpenWorldRoute([new(-350, -460), new(-295, -410), new(-215, -420), new(-145, -365)]));

        for (var inset = 0; inset < 3; inset++)
        {
            var factor = 1 - inset * 0.13;
            var contour = orientationRidge
                .Select(point => new Point(440 + (point.X - 440) * factor, -180 + (point.Y + 180) * factor))
                .ToArray();
            context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Orientation, 38)), Math.Max(0.5, 0.7 * zoom)), ClosedWorldShape(contour, smooth: true));
        }
    }

    private void DrawRoadNetwork(DrawingContext context)
    {
        DrawRoad(context, [new(-500, 145), new(-370, 65), new(-195, 60), new(-50, 34), new(40, 28)]);
        DrawRoad(context, [new(-250, -315), new(-210, -205), new(-110, -120), new(5, -45), new(50, 12)]);
        DrawRoad(context, [new(105, 4), new(220, -15), new(320, -80), new(435, -140)]);
        DrawRoad(context, [new(115, 70), new(245, 150), new(345, 225), new(450, 290)]);
        DrawRoad(context, [new(-520, 250), new(-280, 345), new(0, 350), new(255, 330), new(475, 310)], secondary: true);
    }

    private void DrawRoad(DrawingContext context, Point[] points, bool secondary = false)
    {
        var geometry = OpenWorldRoute(points);
        var outer = secondary ? 9d : 14d;
        var inner = secondary ? 5d : 9d;
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Shadow, 175)), Math.Max(5, outer * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(Road), Math.Max(3, inner * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(RoadEdge, secondary ? (byte)55 : (byte)92)), Math.Max(0.6, 0.9 * zoom)), geometry);
    }

    private void DrawRailNetwork(DrawingContext context)
    {
        var track = new[] { new Point(-770, 395), new(-500, 380), new(-180, 405), new(135, 390), new(445, 310), new(735, 355) };
        var geometry = OpenWorldRoute(track);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Shadow, 155)), Math.Max(4, 6 * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Rail, 190)), Math.Max(1.5, 2.1 * zoom)), geometry);

        if (zoom < MidZoom)
            return;

        var sleeperPen = new Pen(new SolidColorBrush(WithAlpha(Rail, 140)), Math.Max(0.6, 0.8 * zoom));
        for (var index = 0; index < 15; index++)
        {
            var t = index / 14d;
            var point = SamplePolyline(track, t);
            var next = SamplePolyline(track, Math.Min(1, t + 0.02));
            var tangent = Normalize(next - point);
            var normal = new Vector(-tangent.Y, tangent.X);
            context.DrawLine(sleeperPen, Screen(point - normal * 5), Screen(point + normal * 5));
        }
    }

    private void DrawVegetation(DrawingContext context)
    {
        Point[] trees =
        [
            new(-760,-330), new(-710,-305), new(-655,-350), new(-585,-415), new(-510,-455),
            new(-445,-480), new(-420,-205), new(-370,-190), new(-315,-205), new(-125,-505),
            new(-60,-490), new(20,-510), new(150,-465), new(220,-435), new(290,-405),
            new(680,-145), new(715,-80), new(685,-10), new(725,70), new(670,220),
            new(605,430), new(545,465), new(365,500), new(255,510), new(85,490),
            new(-65,505), new(-215,520), new(-365,485), new(-615,415), new(-715,350)
        ];
        foreach (var tree in trees)
            DrawTree(context, tree, 8);

        if (zoom < MidZoom)
            return;

        Point[] localTrees =
        [new(-570,105), new(-540,85), new(-510,110), new(-295,-270), new(-260,-252), new(180,92), new(215,110), new(255,80)];
        foreach (var tree in localTrees)
            DrawTree(context, tree, 6);
    }

    private void DrawVocation(DrawingContext context, IReadOnlySet<string> focus)
    {
        if (!TryService("vocation", out var service, out var center))
            return;
        using (context.PushOpacity(ContextOpacity(service, focus)))
        {
            DrawGlow(context, center, Vocation, service.IsAvailable, 165, 115);
            DrawLocalRoad(context, [center + new Vector(-115, 8), center + new Vector(-15, -5), center + new Vector(115, 12)]);
            DrawLocalRoad(context, [center + new Vector(-25, -100), center + new Vector(-15, -5), center + new Vector(8, 100)]);
            DrawPitchedHouse(context, center + new Vector(-88, -28), 60, 37, Vocation, service.IsAvailable);
            DrawCivicHall(context, center + new Vector(-20, -43), 54, 62, Vocation, service.IsAvailable);
            DrawPitchedHouse(context, center + new Vector(68, -19), 54, 34, Vocation, service.IsAvailable);
            DrawLongHall(context, center + new Vector(75, 58), 88, 32, Vocation, service.IsAvailable);
            DrawPitchedHouse(context, center + new Vector(-74, 62), 49, 29, Vocation, service.IsAvailable);
            if (zoom >= CloseZoom)
                DrawMarket(context, center + new Vector(0, 40), Vocation);
            DrawPlaceName(context, "Vocation", center + new Vector(0, 122), Vocation, service.IsAvailable);
        }
    }

    private void DrawIllumination(DrawingContext context, IReadOnlySet<string> focus)
    {
        if (!TryService("illumination", out var service, out var center))
            return;
        using (context.PushOpacity(ContextOpacity(service, focus)))
        {
            DrawGlow(context, center, Illumination, service.IsAvailable, 160, 120);
            DrawCampusCourt(context, center, Illumination);
            DrawLibraryWing(context, center + new Vector(-82, 12), 80, 31, Illumination, service.IsAvailable);
            DrawLibraryWing(context, center + new Vector(80, 14), 74, 30, Illumination, service.IsAvailable);
            DrawLanternHall(context, center + new Vector(0, -28), Illumination, service.IsAvailable);
            DrawPavilion(context, center + new Vector(-60, 70), Illumination, service.IsAvailable);
            DrawPavilion(context, center + new Vector(62, 70), Illumination, service.IsAvailable);
            if (zoom >= CloseZoom)
                DrawArcade(context, center + new Vector(0, 54), Illumination);
            DrawPlaceName(context, "Illumination", center + new Vector(0, 126), Illumination, service.IsAvailable);
        }
    }

    private void DrawOrientation(DrawingContext context, IReadOnlySet<string> focus)
    {
        if (!TryService("orientation", out var service, out var center))
            return;
        using (context.PushOpacity(ContextOpacity(service, focus)))
        {
            DrawGlow(context, center, Orientation, service.IsAvailable, 165, 120);
            DrawSurveyPlaza(context, center, Orientation);
            DrawSurveyPavilion(context, center + new Vector(-75, 22), Orientation, service.IsAvailable);
            DrawObservationTower(context, center + new Vector(24, -8), Orientation, service.IsAvailable);
            DrawPavilion(context, center + new Vector(84, 50), Orientation, service.IsAvailable);
            DrawSurveyMast(context, center + new Vector(108, -18), Orientation, service.IsAvailable);
            if (zoom >= CloseZoom)
                DrawBearingRose(context, center + new Vector(-8, 67), Orientation);
            DrawPlaceName(context, "Orientation", center + new Vector(0, 128), Orientation, service.IsAvailable);
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
                DrawGlow(context, center, Core, service.IsAvailable, 125, 90);
                DrawPitchedHouse(context, center + new Vector(-36, 2), 47, 30, Core, service.IsAvailable);
                DrawCivicHall(context, center + new Vector(32, -5), 45, 48, Core, service.IsAvailable);
                DrawPlaceName(context, service.Title, center + new Vector(0, 82), Core, service.IsAvailable);
            }
        }
    }

    private void DrawWgtCity(DrawingContext context, IReadOnlySet<string> focus)
    {
        var coreNode = nodes.FirstOrDefault(node => node.IsCore);
        if (coreNode is null || !nodePlaces.TryGetValue(coreNode.NodeId, out var center))
            return;
        using (context.PushOpacity(ContextOpacity(coreNode, focus)))
        {
            DrawGlow(context, center, Core, available: true, 270, 185);
            DrawLocalRoad(context, [center + new Vector(-205, -12), center + new Vector(-80, -8), center + new Vector(65, -4), center + new Vector(210, 10)], 12);
            DrawLocalRoad(context, [center + new Vector(-170, 86), center + new Vector(-35, 80), center + new Vector(105, 85), center + new Vector(198, 92)], 9);
            DrawLocalRoad(context, [center + new Vector(-25, -185), center + new Vector(-12, -65), center + new Vector(0, 65), center + new Vector(16, 160)], 10);
            DrawLocalRoad(context, [center + new Vector(92, -165), center + new Vector(95, -55), center + new Vector(103, 60), center + new Vector(116, 150)], 8);

            DrawCityBlock(context, center + new Vector(-135, -78), 60, 74, Core, 4);
            DrawCityBlock(context, center + new Vector(-58, -110), 64, 108, Core, 6);
            DrawCityBlock(context, center + new Vector(42, -123), 74, 145, Core, 8);
            DrawCityBlock(context, center + new Vector(132, -76), 62, 85, Core, 5);
            DrawCityBlock(context, center + new Vector(-138, 30), 71, 61, Core, 3);
            DrawCityBlock(context, center + new Vector(-48, 50), 74, 80, Core, 4);
            DrawCityBlock(context, center + new Vector(58, 55), 80, 95, Core, 5);
            DrawCityBlock(context, center + new Vector(148, 35), 68, 64, Core, 3);
            DrawCentralTower(context, center + new Vector(35, -2), Core);
            DrawPlaceName(context, "WIIII GOT THIS", center + new Vector(10, 187), Core, true, isCore: true);
        }
    }

    private void DrawConveyance(DrawingContext context, IReadOnlySet<string> focus)
    {
        if (!TryService("conveyance", out var service, out var center))
            return;
        using (context.PushOpacity(ContextOpacity(service, focus)))
        {
            DrawIndustrialGround(context, center, Conveyance, compact: false);
            DrawWarehouse(context, center + new Vector(-86, -8), 88, 35, Conveyance, service.IsAvailable);
            DrawWarehouse(context, center + new Vector(20, 10), 75, 32, Conveyance, service.IsAvailable);
            DrawSilo(context, center + new Vector(88, 24), 16, 44, Conveyance, service.IsAvailable);
            DrawSilo(context, center + new Vector(124, 30), 13, 38, Conveyance, service.IsAvailable);
            DrawRelayMast(context, center + new Vector(42, -28), Conveyance, service.IsAvailable);
            DrawPlaceName(context, "Conveyance", center + new Vector(0, 110), Conveyance, service.IsAvailable);
        }
    }

    private void DrawCapabilityInfrastructure(DrawingContext context, IReadOnlySet<string> focus)
    {
        DrawConveyanceConsumption(context);
        DrawGeospatialConsumption(context, focus);
    }

    private void DrawConveyanceConsumption(DrawingContext context)
    {
        var link = connections.FirstOrDefault(connection =>
            connection.IsEnabled
            && connection.IsCapabilityUse
            && string.Equals(connection.Source.ServiceIdentity?.Value, "vocation", StringComparison.Ordinal)
            && string.Equals(connection.Target.CapabilityIdentity?.Value, BuildAtlasProjectionUseCase.ConveyanceDurableDeliveryCapabilityId, StringComparison.Ordinal));
        if (link is null
            || !TryService("vocation", out var vocation, out var vocationCenter)
            || !TryService("conveyance", out var conveyance, out var conveyanceCenter))
        {
            return;
        }

        var facility = vocationCenter + new Vector(132, 64);
        DrawIndustrialGround(context, facility, Conveyance, compact: true);
        DrawWarehouse(context, facility + new Vector(-20, 5), 45, 23, Conveyance, vocation.IsAvailable && conveyance.IsAvailable);
        DrawRelayMast(context, facility + new Vector(26, 3), Conveyance, vocation.IsAvailable && conveyance.IsAvailable);

        var path = new[] { facility + new Vector(27, 17), new Point(-120, 333), new Point(170, 357), conveyanceCenter + new Vector(-112, 4) };
        var geometry = OpenWorldRoute(path);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Conveyance, 48)), Math.Max(4, 6 * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Conveyance, 155)), Math.Max(0.9, 1.3 * zoom)), geometry);
        if (zoom >= CloseZoom)
            DrawSmallLabel(context, "device relay", facility + new Vector(0, 42), Conveyance);
    }

    private void DrawGeospatialConsumption(DrawingContext context, IReadOnlySet<string> focus)
    {
        var link = connections.FirstOrDefault(connection =>
            connection.IsEnabled
            && connection.IsCapabilityUse
            && string.Equals(connection.Source.ServiceIdentity?.Value, "vocation", StringComparison.Ordinal)
            && string.Equals(connection.Target.CapabilityIdentity?.Value, BuildAtlasProjectionUseCase.OrientationGeospatialCapabilityId, StringComparison.Ordinal));
        if (link is null
            || !TryService("vocation", out _, out var vocationCenter)
            || !TryService("orientation", out _, out var orientationCenter))
        {
            return;
        }

        var focused = selectedNodeId is not null
            && (focus.Contains(link.Source.NodeId) || focus.Contains(link.Target.NodeId));
        if (zoom < CloseZoom && !focused)
            return;

        var sourceTerminal = vocationCenter + new Vector(88, -78);
        var targetTerminal = orientationCenter + new Vector(-78, 50);
        var path = new[] { sourceTerminal, new Point(-215, -25), new Point(35, -76), new Point(235, -110), targetTerminal };
        var geometry = OpenWorldRoute(path);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Orientation, focused ? (byte)72 : (byte)32)), Math.Max(4, 5.5 * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Orientation, focused ? (byte)220 : (byte)110)), Math.Max(0.8, 1.2 * zoom)), geometry);
        DrawGeoTerminal(context, sourceTerminal, Orientation);
        DrawGeoTerminal(context, targetTerminal, Orientation);
        if (focused && !reducedMotion)
            DrawMovingPulse(context, path, Orientation);
        if (zoom >= CloseZoom)
            DrawSmallLabel(context, "geospatial link", sourceTerminal + new Vector(20, 30), Orientation);
    }

    private void DrawCityBlock(DrawingContext context, Point point, double width, double height, Color accent, int rows) =>
        DrawBox(context, point, width, height, 14, accent, powered: true, rows);

    private void DrawCentralTower(DrawingContext context, Point point, Color accent)
    {
        DrawBox(context, point, 64, 170, 20, accent, powered: true, 9);
        DrawBeacon(context, point + new Vector(0, -184), accent, true, 24);
    }

    private void DrawCivicHall(DrawingContext context, Point point, double width, double height, Color accent, bool powered)
    {
        DrawBox(context, point, width, height, 11, accent, powered, 4);
        var p = Screen(point + new Vector(0, -height));
        context.DrawGeometry(new SolidColorBrush(WithAlpha(accent, 105)), null, Polygon([
            new(p.X - width * zoom * 0.43, p.Y), new(p.X, p.Y - 12 * zoom), new(p.X + width * zoom * 0.43, p.Y)
        ]));
    }

    private void DrawPitchedHouse(DrawingContext context, Point point, double width, double height, Color accent, bool powered)
    {
        DrawBox(context, point, width, height, 8, accent, powered, 2);
        var p = Screen(point);
        var roofY = p.Y - height * zoom;
        context.DrawGeometry(new SolidColorBrush(Mix(Color.Parse("#2D3A31"), accent, 0.16)), null, Polygon([
            new(p.X - width * zoom / 2, roofY), new(p.X, roofY - 12 * zoom), new(p.X + width * zoom / 2, roofY)
        ]));
    }

    private void DrawLongHall(DrawingContext context, Point point, double width, double height, Color accent, bool powered) =>
        DrawBox(context, point, width, height, 8, accent, powered, 2);

    private void DrawLibraryWing(DrawingContext context, Point point, double width, double height, Color accent, bool powered)
    {
        DrawBox(context, point, width, height, 8, accent, powered, 2);
        if (zoom < MidZoom)
            return;
        var p = Screen(point);
        context.DrawLine(new Pen(new SolidColorBrush(WithAlpha(WarmLight, powered ? (byte)155 : (byte)45)), 1),
            new Point(p.X - width * zoom * 0.38, p.Y - height * zoom * 0.58),
            new Point(p.X + width * zoom * 0.38, p.Y - height * zoom * 0.58));
    }

    private void DrawLanternHall(DrawingContext context, Point point, Color accent, bool powered)
    {
        DrawBox(context, point, 58, 80, 12, accent, powered, 5);
        var crown = Screen(point + new Vector(0, -94));
        context.DrawGeometry(new SolidColorBrush(WithAlpha(accent, powered ? (byte)180 : (byte)60)), null, Polygon([
            new(crown.X - 17 * zoom, crown.Y + 15 * zoom), new(crown.X, crown.Y), new(crown.X + 17 * zoom, crown.Y + 15 * zoom)
        ]));
        if (powered)
            context.DrawEllipse(Radial(accent, 62), null, crown, 24 * zoom, 18 * zoom);
    }

    private void DrawPavilion(DrawingContext context, Point point, Color accent, bool powered)
    {
        DrawBox(context, point, 40, 25, 7, accent, powered, 1);
        var p = Screen(point);
        context.DrawLine(new Pen(new SolidColorBrush(WithAlpha(accent, 90)), 0.8),
            new Point(p.X - 23 * zoom, p.Y - 28 * zoom), new Point(p.X + 23 * zoom, p.Y - 28 * zoom));
    }

    private void DrawSurveyPavilion(DrawingContext context, Point point, Color accent, bool powered)
    {
        DrawBox(context, point, 84, 32, 9, accent, powered, 2);
        if (zoom >= MidZoom)
        {
            var p = Screen(point);
            context.DrawLine(new Pen(new SolidColorBrush(WithAlpha(accent, 120)), 1),
                new Point(p.X - 36 * zoom, p.Y - 39 * zoom), new Point(p.X + 36 * zoom, p.Y - 39 * zoom));
        }
    }

    private void DrawObservationTower(DrawingContext context, Point point, Color accent, bool powered)
    {
        DrawBox(context, point, 42, 100, 10, accent, powered, 6);
        var deck = Screen(point + new Vector(0, -106));
        context.FillRectangle(new SolidColorBrush(WithAlpha(accent, 128)), new Rect(deck.X - 29 * zoom, deck.Y, 58 * zoom, 5 * zoom));
        DrawBeacon(context, point + new Vector(0, -120), accent, powered, 18);
    }

    private void DrawSurveyMast(DrawingContext context, Point point, Color accent, bool powered)
    {
        var p = Screen(point);
        var pen = new Pen(new SolidColorBrush(WithAlpha(accent, powered ? (byte)190 : (byte)70)), Math.Max(0.8, 1 * zoom));
        context.DrawLine(pen, p, new Point(p.X, p.Y - 64 * zoom));
        context.DrawLine(pen, p, new Point(p.X - 15 * zoom, p.Y - 4 * zoom));
        context.DrawLine(pen, p, new Point(p.X + 15 * zoom, p.Y - 4 * zoom));
        if (powered)
            context.DrawEllipse(Radial(accent, 60), null, new Point(p.X, p.Y - 65 * zoom), 18 * zoom, 18 * zoom);
    }

    private void DrawWarehouse(DrawingContext context, Point point, double width, double height, Color accent, bool powered)
    {
        DrawBox(context, point, width, height, 8, accent, powered, 1);
        if (zoom < MidZoom)
            return;
        var p = Screen(point);
        context.FillRectangle(new SolidColorBrush(WithAlpha(Shadow, 205)), new Rect(
            p.X - width * zoom * 0.15, p.Y - height * zoom * 0.42,
            width * zoom * 0.30, height * zoom * 0.42));
    }

    private void DrawBox(DrawingContext context, Point baseWorld, double width, double height, double depth, Color accent, bool powered, int rows)
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
        context.DrawGeometry(new SolidColorBrush(body), new Pen(new SolidColorBrush(WithAlpha(accent, 62)), 0.7),
            Polygon([new(left, top), new(right, top), new(right, p.Y), new(left, p.Y)]));
        context.DrawGeometry(new SolidColorBrush(Mix(body, Shadow, 0.30)), null, Polygon([
            new(right, top), new(right + d * 0.65, top - d * 0.45),
            new(right + d * 0.65, p.Y - d * 0.45), new(right, p.Y)
        ]));
        context.DrawGeometry(new SolidColorBrush(Mix(body, accent, 0.22)), null, Polygon([
            new(left, top), new(left + d * 0.65, top - d * 0.45),
            new(right + d * 0.65, top - d * 0.45), new(right, top)
        ]));

        if (zoom < MidZoom)
            return;

        var rowCount = Math.Clamp(rows, 1, 9);
        var colCount = width >= 58 ? 3 : 2;
        for (var row = 0; row < rowCount; row++)
        {
            var y = top + (row + 1) * h / (rowCount + 1);
            for (var col = 0; col < colCount; col++)
            {
                var x = left + (col + 1) * w / (colCount + 1);
                context.FillRectangle(new SolidColorBrush(WithAlpha(powered ? WarmLight : Muted, powered ? (byte)200 : (byte)55)),
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
            context.DrawEllipse(new SolidColorBrush(WarmLight), null, new Point(p.X + r * 0.45, p.Y - h * 0.65), 1.4 * zoom, 1.4 * zoom);
    }

    private void DrawRelayMast(DrawingContext context, Point point, Color accent, bool powered)
    {
        var p = Screen(point);
        var pen = new Pen(new SolidColorBrush(WithAlpha(accent, powered ? (byte)200 : (byte)70)), Math.Max(0.8, 1 * zoom));
        context.DrawLine(pen, p, new Point(p.X, p.Y - 60 * zoom));
        context.DrawLine(pen, p, new Point(p.X - 15 * zoom, p.Y - 5 * zoom));
        context.DrawLine(pen, p, new Point(p.X + 15 * zoom, p.Y - 5 * zoom));
        DrawBeacon(context, point + new Vector(0, -62), accent, powered, 18);
    }

    private void DrawIndustrialGround(DrawingContext context, Point center, Color accent, bool compact)
    {
        var x = compact ? 74d : 170d;
        var y = compact ? 48d : 108d;
        var p = Screen(center);
        context.DrawGeometry(new SolidColorBrush(WithAlpha(Color.Parse("#171820"), 232)),
            new Pen(new SolidColorBrush(WithAlpha(accent, 60)), Math.Max(0.7, 0.9 * zoom)),
            Polygon([
                new(p.X - x * zoom, p.Y - y * zoom), new(p.X + x * 0.82 * zoom, p.Y - y * 0.88 * zoom),
                new(p.X + x * zoom, p.Y + y * 0.35 * zoom), new(p.X + x * 0.45 * zoom, p.Y + y * 0.82 * zoom),
                new(p.X - x * 0.86 * zoom, p.Y + y * 0.65 * zoom)
            ]));
    }

    private void DrawCampusCourt(DrawingContext context, Point center, Color accent)
    {
        var p = Screen(center + new Vector(0, 28));
        context.DrawEllipse(new SolidColorBrush(WithAlpha(Color.Parse("#6A6048"), 65)),
            new Pen(new SolidColorBrush(WithAlpha(accent, 55)), 0.7), p, 58 * zoom, 34 * zoom);
    }

    private void DrawSurveyPlaza(DrawingContext context, Point center, Color accent)
    {
        var points = new[]
        {
            center + new Vector(-72, 35), center + new Vector(-20, 12), center + new Vector(67, 25),
            center + new Vector(84, 72), center + new Vector(-36, 90)
        };
        context.DrawGeometry(new SolidColorBrush(WithAlpha(Color.Parse("#233338"), 150)),
            new Pen(new SolidColorBrush(WithAlpha(accent, 60)), 0.7), ClosedWorldShape(points, smooth: false));
    }

    private void DrawMarket(DrawingContext context, Point point, Color accent)
    {
        var p = Screen(point);
        context.DrawGeometry(new SolidColorBrush(WithAlpha(accent, 74)), null, Polygon([
            new(p.X - 26 * zoom, p.Y), new(p.X - 18 * zoom, p.Y - 14 * zoom),
            new(p.X + 23 * zoom, p.Y - 14 * zoom), new(p.X + 29 * zoom, p.Y)
        ]));
    }

    private void DrawArcade(DrawingContext context, Point point, Color accent)
    {
        var p = Screen(point);
        var pen = new Pen(new SolidColorBrush(WithAlpha(accent, 95)), Math.Max(0.6, 0.8 * zoom));
        for (var index = -3; index <= 3; index++)
        {
            var x = p.X + index * 13 * zoom;
            context.DrawLine(pen, new Point(x, p.Y), new Point(x, p.Y - 18 * zoom));
        }
        context.DrawLine(pen, new Point(p.X - 46 * zoom, p.Y - 18 * zoom), new Point(p.X + 46 * zoom, p.Y - 18 * zoom));
    }

    private void DrawBearingRose(DrawingContext context, Point point, Color accent)
    {
        var p = Screen(point);
        var pen = new Pen(new SolidColorBrush(WithAlpha(accent, 95)), Math.Max(0.6, 0.8 * zoom));
        context.DrawEllipse(null, pen, p, 22 * zoom, 22 * zoom);
        context.DrawLine(pen, new Point(p.X, p.Y - 30 * zoom), new Point(p.X, p.Y + 30 * zoom));
        context.DrawLine(pen, new Point(p.X - 30 * zoom, p.Y), new Point(p.X + 30 * zoom, p.Y));
    }

    private void DrawGeoTerminal(DrawingContext context, Point point, Color accent)
    {
        var p = Screen(point);
        context.DrawEllipse(new SolidColorBrush(Color.Parse("#0C2027")), new Pen(new SolidColorBrush(WithAlpha(accent, 165)), 0.9), p, 10 * zoom, 10 * zoom);
        context.DrawLine(new Pen(new SolidColorBrush(WithAlpha(accent, 170)), 0.8),
            new Point(p.X, p.Y - 16 * zoom), new Point(p.X, p.Y + 16 * zoom));
    }

    private void DrawBeacon(DrawingContext context, Point point, Color accent, bool powered, double radius)
    {
        if (!powered)
            return;
        var p = Screen(point);
        context.DrawEllipse(Radial(accent, 58), null, p, radius * zoom, radius * zoom);
        context.DrawEllipse(new SolidColorBrush(WithAlpha(accent, 240)), null, p, 1.8 * zoom, 1.8 * zoom);
    }

    private void DrawGlow(DrawingContext context, Point center, Color accent, bool available, double radiusX, double radiusY)
    {
        var p = Screen(center);
        context.DrawEllipse(Radial(accent, available ? (byte)40 : (byte)15), null, p, radiusX * zoom, radiusY * zoom);
    }

    private void DrawTree(DrawingContext context, Point point, double size)
    {
        if (zoom < 0.62)
            return;
        var p = Screen(point);
        context.DrawLine(new Pen(new SolidColorBrush(Color.Parse("#65523B")), Math.Max(0.7, 0.9 * zoom)), p, new Point(p.X, p.Y + size * 1.2 * zoom));
        context.DrawEllipse(new SolidColorBrush(WithAlpha(Shadow, 72)), null, new Point(p.X + 3 * zoom, p.Y + 5 * zoom), size * 0.9 * zoom, size * 0.5 * zoom);
        context.DrawEllipse(new SolidColorBrush(Color.Parse("#1B4A32")), null, new Point(p.X - 2 * zoom, p.Y), size * 0.82 * zoom, size * 0.95 * zoom);
        context.DrawEllipse(new SolidColorBrush(Color.Parse("#2E7650")), null, new Point(p.X + 3 * zoom, p.Y - 3 * zoom), size * 0.55 * zoom, size * 0.65 * zoom);
    }

    private void DrawLocalRoad(DrawingContext context, Point[] points, double width = 6)
    {
        var geometry = OpenWorldRoute(points);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(Shadow, 155)), Math.Max(4, (width + 4) * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(Road), Math.Max(2, width * zoom)), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(RoadEdge, 70)), Math.Max(0.5, 0.7 * zoom)), geometry);
    }

    private void DrawPlaceName(DrawingContext context, string title, Point point, Color accent, bool available, bool isCore = false)
    {
        var p = Screen(point);
        DrawCenteredText(context, title, p, Math.Clamp((isCore ? 13.3 : 11.3) * zoom, 9, isCore ? 15.5 : 13.2), Text);
        if (zoom >= MidZoom)
        {
            var stateColor = available ? accent : Muted;
            context.DrawEllipse(new SolidColorBrush(WithAlpha(stateColor, 190)), null,
                new Point(p.X, p.Y + 18 * zoom), 2.1 * zoom, 2.1 * zoom);
        }
    }

    private void DrawSmallLabel(DrawingContext context, string text, Point point, Color accent) =>
        DrawCenteredText(context, text, Screen(point), Math.Clamp(7.4 * zoom, 6.5, 9), WithAlpha(accent, 175));

    private void DrawContour(DrawingContext context, Point[] line, byte alpha) =>
        context.DrawGeometry(null, new Pen(new SolidColorBrush(WithAlpha(LandEdge, alpha)), Math.Max(0.5, 0.7 * zoom)), OpenWorldRoute(line));

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
        return 0.40;
    }

    private AtlasNodePresentationViewModel? HitTestPlace(Point screenPoint)
    {
        foreach (var node in nodes.Where(node => node.IsCore || node.IsService).OrderByDescending(node => node.IsCore))
        {
            if (!nodePlaces.TryGetValue(node.NodeId, out var world))
                continue;
            var center = Screen(world);
            var radius = node.IsCore
                ? Math.Max(70, 190 * zoom)
                : node.IsSharedCapabilityProvider
                    ? Math.Max(48, 120 * zoom)
                    : Math.Max(50, 125 * zoom);
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
        brush.GradientStops.Add(new GradientStop(WithAlpha(accent, (byte)(alpha / 4)), 0.52));
        brush.GradientStops.Add(new GradientStop(WithAlpha(accent, 0), 1));
        return brush;
    }

    private static Color Mix(Color from, Color to, double t) => Color.FromArgb(
        (byte)Math.Clamp(Math.Round(from.A + (to.A - from.A) * t), 0, 255),
        (byte)Math.Clamp(Math.Round(from.R + (to.R - from.R) * t), 0, 255),
        (byte)Math.Clamp(Math.Round(from.G + (to.G - from.G) * t), 0, 255),
        (byte)Math.Clamp(Math.Round(from.B + (to.B - from.B) * t), 0, 255));

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
