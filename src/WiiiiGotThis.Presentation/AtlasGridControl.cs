using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

/// <summary>
/// Abstract, deterministic Atlas renderer for the flagship World/Atlas theme.
/// Products occupy a calm modular lattice; shared capability providers live on a lower
/// infrastructure bus rather than masquerading as peer products. Presentation only.
/// </summary>
public sealed class AtlasGridControl : Control
{
    private const double WorldCenterX = 1000d;
    private const double WorldCenterY = 660d;
    private const double ProductWidth = 232d;
    private const double ProductHeight = 104d;
    private const double CoreWidth = 276d;
    private const double CoreHeight = 118d;
    private const double InfrastructureWidth = 218d;
    private const double InfrastructureHeight = 72d;
    private const double CapabilityWidth = 156d;
    private const double CapabilityHeight = 34d;
    private const double DetailZoom = 0.92d;

    private static readonly Color BackgroundTop = Color.Parse("#071015");
    private static readonly Color BackgroundBottom = Color.Parse("#020609");
    private static readonly Color GridMinor = Color.Parse("#18313D");
    private static readonly Color GridMajor = Color.Parse("#2B5262");
    private static readonly Color GridPoint = Color.Parse("#315F70");
    private static readonly Color Surface = Color.Parse("#0A151B");
    private static readonly Color SurfaceRaised = Color.Parse("#0D1D25");
    private static readonly Color Text = Color.Parse("#EDF7FA");
    private static readonly Color Muted = Color.Parse("#8EA7B0");
    private static readonly Color CoreAccent = Color.Parse("#72E5C2");
    private static readonly Color VocationAccent = Color.Parse("#67D69C");
    private static readonly Color IlluminationAccent = Color.Parse("#E0BD68");
    private static readonly Color OrientationAccent = Color.Parse("#70C4EA");
    private static readonly Color ConveyanceAccent = Color.Parse("#B395E6");
    private static readonly Color GenericAccent = Color.Parse("#78BFD1");

    private static readonly Dictionary<string, int> PreferredProductOrder = new(StringComparer.Ordinal)
    {
        ["vocation"] = 0,
        ["illumination"] = 1,
        ["orientation"] = 2
    };

    private IReadOnlyList<AtlasNodePresentationViewModel> nodes = Array.Empty<AtlasNodePresentationViewModel>();
    private IReadOnlyList<AtlasConnectionPresentationViewModel> connections = Array.Empty<AtlasConnectionPresentationViewModel>();
    private readonly Dictionary<string, Point> nodePlaces = new(StringComparer.Ordinal);
    private readonly Dictionary<string, double> interactionVisuals = new(StringComparer.Ordinal);
    private string? selectedNodeId;
    private string? hoverNodeId;
    private string? pressedNodeId;
    private int pressedClickCount;
    private bool reducedMotion;
    private double zoom = 0.82d;
    private double translateX;
    private double translateY;

    public AtlasGridControl()
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
        DrawBackground(context);
        if (nodes.Count == 0)
            return;

        var focus = AtlasPresentationFocus.Build(connections, selectedNodeId);
        DrawGrid(context);
        DrawAmbientField(context);
        DrawInfrastructureBus(context, focus);
        DrawRelationshipTraces(context, focus);
        DrawPrimaryNodes(context, focus);
        DrawVisibleCapabilityPorts(context, focus);
        DrawForegroundVignette(context);
        RequestSceneFrame();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var node = HitTestNode(e.GetPosition(this));
        var next = node?.NodeId;
        if (!string.Equals(next, hoverNodeId, StringComparison.Ordinal))
        {
            hoverNodeId = next;
            InvalidateVisual();
            RequestSceneFrame();
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (hoverNodeId is null)
            return;
        hoverNodeId = null;
        InvalidateVisual();
        RequestSceneFrame();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        var node = HitTestNode(e.GetPosition(this));
        if (node is null)
            return;

        Focus();
        pressedNodeId = node.NodeId;
        pressedClickCount = e.ClickCount;
        e.Pointer.Capture(this);
        InvalidateVisual();
        RequestSceneFrame();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (pressedNodeId is null)
            return;

        var pressed = pressedNodeId;
        var clickCount = pressedClickCount;
        pressedNodeId = null;
        pressedClickCount = 0;
        e.Pointer.Capture(null);

        var node = HitTestNode(e.GetPosition(this));
        if (node is not null && string.Equals(node.NodeId, pressed, StringComparison.Ordinal))
        {
            NodeInvoked?.Invoke(node);
            if (clickCount >= 2 && node.CanOpenProductSurface)
                NodeActivated?.Invoke(node);
        }

        InvalidateVisual();
        RequestSceneFrame();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter && SelectedNode() is { } selected)
        {
            if (selected.CanOpenProductSurface)
                NodeActivated?.Invoke(selected);
            else
                NodeInvoked?.Invoke(selected);
            e.Handled = true;
            return;
        }

        var direction = e.Key switch
        {
            Key.Left => new Vector(-1, 0),
            Key.Right => new Vector(1, 0),
            Key.Up => new Vector(0, -1),
            Key.Down => new Vector(0, 1),
            _ => default
        };
        if (direction != default && MoveSelection(direction))
        {
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private void RebuildPlaces()
    {
        nodePlaces.Clear();

        if (nodes.FirstOrDefault(node => node.IsCore) is { } core)
            nodePlaces[core.NodeId] = new Point(0, -180);

        var products = nodes
            .Where(node => node.IsPrimaryProductProvider)
            .OrderBy(node => ProductOrder(node.ServiceIdentity?.Value))
            .ThenBy(node => node.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(node => node.NodeId, StringComparer.Ordinal)
            .ToArray();

        for (var index = 0; index < products.Length; index++)
            nodePlaces[products[index].NodeId] = ProductSlot(index, products.Length);

        var infrastructure = nodes
            .Where(node => node.IsSharedCapabilityProvider)
            .OrderBy(node => node.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(node => node.NodeId, StringComparer.Ordinal)
            .ToArray();
        for (var index = 0; index < infrastructure.Length; index++)
        {
            var offset = (index - (infrastructure.Length - 1) / 2d) * 270d;
            nodePlaces[infrastructure[index].NodeId] = new Point(offset, 390);
        }

        foreach (var service in nodes.Where(node => node.IsService))
        {
            if (!nodePlaces.TryGetValue(service.NodeId, out var owner))
                continue;

            var capabilities = nodes
                .Where(node => node.IsCapability && node.ServiceIdentity == service.ServiceIdentity)
                .OrderBy(node => node.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(node => node.NodeId, StringComparer.Ordinal)
                .ToArray();
            for (var index = 0; index < capabilities.Length; index++)
            {
                var x = owner.X + (index - (capabilities.Length - 1) / 2d) * 172d;
                var y = owner.Y + (service.IsSharedCapabilityProvider ? 70d : 92d);
                nodePlaces[capabilities[index].NodeId] = new Point(x, y);
            }
        }
    }

    private static int ProductOrder(string? serviceId) =>
        serviceId is not null && PreferredProductOrder.TryGetValue(serviceId, out var order)
            ? order
            : 100;

    private static Point ProductSlot(int index, int count)
    {
        const int maxColumns = 4;
        const double columnGap = 292d;
        const double rowGap = 188d;
        const double firstRowY = 55d;
        var row = index / maxColumns;
        var rowStart = row * maxColumns;
        var rowCount = Math.Min(maxColumns, count - rowStart);
        var column = index - rowStart;
        var x = (column - (rowCount - 1) / 2d) * columnGap;
        return new Point(x, firstRowY + row * rowGap);
    }

    private void DrawBackground(DrawingContext context)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative)
        };
        brush.GradientStops.Add(new GradientStop(BackgroundTop, 0));
        brush.GradientStops.Add(new GradientStop(Color.Parse("#051017"), 0.5));
        brush.GradientStops.Add(new GradientStop(BackgroundBottom, 1));
        context.FillRectangle(brush, new Rect(Bounds.Size));
    }

    private void DrawGrid(DrawingContext context)
    {
        const double spacing = 56d;
        const int majorEvery = 4;
        var leftWorld = (-translateX / Math.Max(zoom, 0.01)) - WorldCenterX;
        var topWorld = (-translateY / Math.Max(zoom, 0.01)) - WorldCenterY;
        var rightWorld = leftWorld + Bounds.Width / Math.Max(zoom, 0.01);
        var bottomWorld = topWorld + Bounds.Height / Math.Max(zoom, 0.01);
        var startX = Math.Floor(leftWorld / spacing) * spacing;
        var startY = Math.Floor(topWorld / spacing) * spacing;

        for (var x = startX; x <= rightWorld; x += spacing)
        {
            var major = Math.Abs((int)Math.Round(x / spacing)) % majorEvery == 0;
            var color = major ? GridMajor : GridMinor;
            var alpha = major ? (byte)60 : (byte)30;
            var sx = Screen(new Point(x, 0)).X;
            context.DrawLine(new Pen(new SolidColorBrush(WithAlpha(color, alpha)), major ? 0.9 : 0.55),
                new Point(sx, 0), new Point(sx, Bounds.Height));
        }

        for (var y = startY; y <= bottomWorld; y += spacing)
        {
            var major = Math.Abs((int)Math.Round(y / spacing)) % majorEvery == 0;
            var color = major ? GridMajor : GridMinor;
            var alpha = major ? (byte)60 : (byte)30;
            var sy = Screen(new Point(0, y)).Y;
            context.DrawLine(new Pen(new SolidColorBrush(WithAlpha(color, alpha)), major ? 0.9 : 0.55),
                new Point(0, sy), new Point(Bounds.Width, sy));
        }

        if (zoom < 0.72)
            return;

        for (var x = startX; x <= rightWorld; x += spacing * majorEvery)
        {
            for (var y = startY; y <= bottomWorld; y += spacing * majorEvery)
            {
                var p = Screen(new Point(x, y));
                context.DrawEllipse(new SolidColorBrush(WithAlpha(GridPoint, 72)), null, p, 1.1, 1.1);
            }
        }
    }

    private void DrawAmbientField(DrawingContext context)
    {
        var bandTop = Screen(new Point(0, -285)).Y;
        var bandBottom = Screen(new Point(0, 300)).Y;
        var rect = new Rect(0, bandTop, Bounds.Width, Math.Max(0, bandBottom - bandTop));
        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative)
        };
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 66, 159, 182), 0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(22, 66, 159, 182), 0.5));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 66, 159, 182), 1));
        context.FillRectangle(brush, rect);

        var axisY = Screen(new Point(0, 320)).Y;
        context.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(58, 107, 184, 202)), 1.2),
            new Point(Math.Max(0, Screen(new Point(-720, 0)).X), axisY),
            new Point(Math.Min(Bounds.Width, Screen(new Point(720, 0)).X), axisY));
    }

    private void DrawInfrastructureBus(DrawingContext context, IReadOnlySet<string> focus)
    {
        var providers = nodes.Where(node => node.IsSharedCapabilityProvider).ToArray();
        if (providers.Length == 0)
            return;

        var y = Screen(new Point(0, 390)).Y;
        var x1 = Screen(new Point(-710, 0)).X;
        var x2 = Screen(new Point(710, 0)).X;
        context.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(62, 179, 149, 230)), Math.Max(1, 1.3 * zoom)),
            new Point(x1, y), new Point(x2, y));
        context.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(24, 179, 149, 230)), Math.Max(5, 8 * zoom)),
            new Point(x1, y), new Point(x2, y));
    }

    private void DrawRelationshipTraces(DrawingContext context, IReadOnlySet<string> focus)
    {
        if (selectedNodeId is null)
            return;

        foreach (var connection in connections)
        {
            if (!connection.IsEnabled)
                continue;
            if (!focus.Contains(connection.Source.NodeId) || !focus.Contains(connection.Target.NodeId))
                continue;
            if (!nodePlaces.TryGetValue(connection.Source.NodeId, out var source)
                || !nodePlaces.TryGetValue(connection.Target.NodeId, out var target))
            {
                continue;
            }

            if (connection.Kind == AtlasConnectionKind.Composition
                && !string.Equals(selectedNodeId, BuildAtlasProjectionUseCase.CoreNodeId, StringComparison.Ordinal))
            {
                continue;
            }

            var sourcePoint = Screen(source);
            var targetPoint = Screen(target);
            var accent = connection.IsCapabilityUse
                ? AccentFor(connection.Target)
                : CoreAccent;
            var alpha = connection.IsCapabilityUse ? (byte)155 : (byte)72;
            DrawOrthogonalTrace(context, sourcePoint, targetPoint, WithAlpha(accent, alpha), connection.IsCapabilityUse ? 1.8 : 1.1);
        }
    }

    private void DrawOrthogonalTrace(DrawingContext context, Point source, Point target, Color color, double thickness)
    {
        var midY = source.Y + (target.Y - source.Y) * 0.5;
        var pen = new Pen(new SolidColorBrush(color), Math.Max(0.9, thickness * zoom));
        context.DrawLine(pen, source, new Point(source.X, midY));
        context.DrawLine(pen, new Point(source.X, midY), new Point(target.X, midY));
        context.DrawLine(pen, new Point(target.X, midY), target);
        context.DrawEllipse(new SolidColorBrush(WithAlpha(color, 210)), null, new Point(target.X, midY), 2.2, 2.2);
    }

    private void DrawPrimaryNodes(DrawingContext context, IReadOnlySet<string> focus)
    {
        foreach (var node in nodes.Where(node => node.IsCore || node.IsService))
        {
            if (!nodePlaces.TryGetValue(node.NodeId, out var world))
                continue;

            if (node.IsSharedCapabilityProvider)
                DrawInfrastructureNode(context, node, world, focus);
            else
                DrawProductNode(context, node, world, focus);
        }
    }

    private void DrawProductNode(
        DrawingContext context,
        AtlasNodePresentationViewModel node,
        Point world,
        IReadOnlySet<string> focus)
    {
        var center = Screen(world);
        var accent = AccentFor(node);
        var isSelected = string.Equals(node.NodeId, selectedNodeId, StringComparison.Ordinal);
        var isHovered = string.Equals(node.NodeId, hoverNodeId, StringComparison.Ordinal);
        var isPressed = string.Equals(node.NodeId, pressedNodeId, StringComparison.Ordinal);
        var focused = selectedNodeId is null || focus.Contains(node.NodeId);
        var interaction = UpdateInteraction(node.NodeId, isPressed ? -1d : isHovered ? 1d : isSelected ? 0.52d : 0d);
        var scale = reducedMotion ? 1d : 1d + interaction * 0.018d;
        var width = (node.IsCore ? CoreWidth : ProductWidth) * zoom * scale;
        var height = (node.IsCore ? CoreHeight : ProductHeight) * zoom * scale;
        var rect = new Rect(center.X - width / 2, center.Y - height / 2, width, height);
        var opacity = focused ? (node.IsEnabled ? 1d : 0.48d) : 0.22d;
        var radius = Math.Max(12, (node.IsCore ? 24 : 18) * zoom);

        var shadowRect = new Rect(rect.X + 0, rect.Y + Math.Max(5, 9 * zoom), rect.Width, rect.Height);
        context.DrawGeometry(new SolidColorBrush(Color.FromArgb((byte)(110 * opacity), 0, 0, 0)), null, RoundedRect(shadowRect, radius));

        if (isSelected || isHovered)
        {
            var glow = rect.Inflate(Math.Max(5, 9 * zoom));
            context.DrawGeometry(new SolidColorBrush(WithAlpha(accent, (byte)((isSelected ? 34 : 22) * opacity))), null, RoundedRect(glow, radius + 8 * zoom));
        }

        var fill = isPressed ? Mix(Surface, accent, 0.10) : isHovered ? SurfaceRaised : Surface;
        context.DrawGeometry(new SolidColorBrush(WithAlpha(fill, (byte)(245 * opacity))),
            new Pen(new SolidColorBrush(WithAlpha(accent, (byte)((isSelected ? 230 : isHovered ? 180 : 105) * opacity))),
                Math.Max(1, (isSelected ? 2.0 : 1.15) * zoom)),
            RoundedRect(rect, radius));

        var inner = rect.Deflate(Math.Max(7, 10 * zoom));
        context.DrawGeometry(null,
            new Pen(new SolidColorBrush(WithAlpha(accent, (byte)(38 * opacity))), Math.Max(0.6, 0.8 * zoom)),
            RoundedRect(inner, Math.Max(8, radius - 6 * zoom)));

        var glyphCenter = new Point(rect.Left + 34 * zoom, rect.Center.Y);
        DrawNodeGlyph(context, glyphCenter, accent, node.IsCore ? 12 : 9, opacity);

        var titleX = rect.Left + 58 * zoom;
        var titleY = rect.Center.Y - (node.IsCore ? 15 : 14) * zoom;
        DrawText(context, node.Title, new Point(titleX, titleY), Math.Max(12, (node.IsCore ? 17 : 15) * zoom), WithAlpha(Text, (byte)(255 * opacity)), FontWeight.SemiBold);

        if (zoom >= 0.74)
        {
            var subtitle = node.IsCore ? "Atlas" : node.Subtitle;
            DrawText(context, subtitle, new Point(titleX, rect.Center.Y + 7 * zoom), Math.Max(8.5, 10.5 * zoom), WithAlpha(Muted, (byte)(190 * opacity)), FontWeight.Normal);
        }

        DrawStatusDot(context, new Point(rect.Right - 18 * zoom, rect.Top + 18 * zoom), node, accent, opacity);
    }

    private void DrawInfrastructureNode(
        DrawingContext context,
        AtlasNodePresentationViewModel node,
        Point world,
        IReadOnlySet<string> focus)
    {
        var center = Screen(world);
        var accent = AccentFor(node);
        var isSelected = string.Equals(node.NodeId, selectedNodeId, StringComparison.Ordinal);
        var isHovered = string.Equals(node.NodeId, hoverNodeId, StringComparison.Ordinal);
        var isPressed = string.Equals(node.NodeId, pressedNodeId, StringComparison.Ordinal);
        var focused = selectedNodeId is null || focus.Contains(node.NodeId);
        var interaction = UpdateInteraction(node.NodeId, isPressed ? -1d : isHovered ? 1d : isSelected ? 0.45d : 0d);
        var scale = reducedMotion ? 1d : 1d + interaction * 0.014d;
        var rect = new Rect(
            center.X - InfrastructureWidth * zoom * scale / 2,
            center.Y - InfrastructureHeight * zoom * scale / 2,
            InfrastructureWidth * zoom * scale,
            InfrastructureHeight * zoom * scale);
        var opacity = focused ? 1d : 0.24d;
        var radius = Math.Max(10, 14 * zoom);

        context.DrawGeometry(new SolidColorBrush(WithAlpha(Color.Parse("#0B1018"), (byte)(245 * opacity))),
            new Pen(new SolidColorBrush(WithAlpha(accent, (byte)((isSelected ? 215 : isHovered ? 165 : 95) * opacity))), Math.Max(1, 1.15 * zoom)),
            RoundedRect(rect, radius));
        context.DrawLine(new Pen(new SolidColorBrush(WithAlpha(accent, (byte)(170 * opacity))), Math.Max(1, 1.6 * zoom)),
            new Point(rect.Left + 14 * zoom, rect.Bottom - 12 * zoom),
            new Point(rect.Right - 14 * zoom, rect.Bottom - 12 * zoom));
        DrawNodeGlyph(context, new Point(rect.Left + 28 * zoom, rect.Center.Y), accent, 7.5, opacity);
        DrawText(context, node.Title, new Point(rect.Left + 48 * zoom, rect.Center.Y - 8 * zoom), Math.Max(10, 12.5 * zoom), WithAlpha(Text, (byte)(240 * opacity)), FontWeight.SemiBold);
        DrawStatusDot(context, new Point(rect.Right - 16 * zoom, rect.Top + 16 * zoom), node, accent, opacity);
    }

    private void DrawVisibleCapabilityPorts(DrawingContext context, IReadOnlySet<string> focus)
    {
        foreach (var capability in nodes.Where(node => node.IsCapability))
        {
            if (!nodePlaces.TryGetValue(capability.NodeId, out var world))
                continue;

            var ownerSelected = connections.Any(connection =>
                connection.Kind == AtlasConnectionKind.CapabilityOwnership
                && string.Equals(connection.Target.NodeId, capability.NodeId, StringComparison.Ordinal)
                && string.Equals(connection.Source.NodeId, selectedNodeId, StringComparison.Ordinal));
            var capabilitySelected = string.Equals(capability.NodeId, selectedNodeId, StringComparison.Ordinal);
            if (zoom < DetailZoom && !ownerSelected && !capabilitySelected)
                continue;
            if (selectedNodeId is not null && !focus.Contains(capability.NodeId) && !ownerSelected && !capabilitySelected)
                continue;

            var center = Screen(world);
            var accent = AccentFor(capability);
            var rect = new Rect(
                center.X - CapabilityWidth * zoom / 2,
                center.Y - CapabilityHeight * zoom / 2,
                CapabilityWidth * zoom,
                CapabilityHeight * zoom);
            context.DrawGeometry(new SolidColorBrush(Color.FromArgb(222, 7, 16, 22)),
                new Pen(new SolidColorBrush(WithAlpha(accent, capabilitySelected ? (byte)205 : (byte)95)), Math.Max(0.8, 0.95 * zoom)),
                RoundedRect(rect, Math.Max(8, 11 * zoom)));
            context.DrawEllipse(new SolidColorBrush(WithAlpha(accent, 210)), null,
                new Point(rect.Left + 16 * zoom, rect.Center.Y), 2.8 * zoom, 2.8 * zoom);
            DrawText(context, capability.Title, new Point(rect.Left + 27 * zoom, rect.Center.Y - 6 * zoom), Math.Max(8, 9.5 * zoom), WithAlpha(Text, 215), FontWeight.Medium);
        }
    }

    private void DrawNodeGlyph(DrawingContext context, Point center, Color accent, double radius, double opacity)
    {
        var r = Math.Max(4, radius * zoom);
        context.DrawEllipse(new SolidColorBrush(WithAlpha(accent, (byte)(38 * opacity))),
            new Pen(new SolidColorBrush(WithAlpha(accent, (byte)(220 * opacity))), Math.Max(1, 1.2 * zoom)), center, r, r);
        context.DrawEllipse(new SolidColorBrush(WithAlpha(accent, (byte)(240 * opacity))), null, center, Math.Max(1.6, 2.1 * zoom), Math.Max(1.6, 2.1 * zoom));
        context.DrawLine(new Pen(new SolidColorBrush(WithAlpha(accent, (byte)(120 * opacity))), Math.Max(0.7, 0.8 * zoom)),
            new Point(center.X - r * 1.55, center.Y), new Point(center.X + r * 1.55, center.Y));
        context.DrawLine(new Pen(new SolidColorBrush(WithAlpha(accent, (byte)(120 * opacity))), Math.Max(0.7, 0.8 * zoom)),
            new Point(center.X, center.Y - r * 1.55), new Point(center.X, center.Y + r * 1.55));
    }

    private void DrawStatusDot(DrawingContext context, Point center, AtlasNodePresentationViewModel node, Color accent, double opacity)
    {
        var color = !node.IsEnabled || !node.IsIntegrated
            ? Muted
            : node.IsAvailable
                ? accent
                : Color.Parse("#D7A968");
        context.DrawEllipse(new SolidColorBrush(WithAlpha(color, (byte)(215 * opacity))), null, center, Math.Max(2.2, 3 * zoom), Math.Max(2.2, 3 * zoom));
    }

    private void DrawForegroundVignette(DrawingContext context)
    {
        var top = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative)
        };
        top.GradientStops.Add(new GradientStop(Color.FromArgb(105, 0, 0, 0), 0));
        top.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.22));
        top.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.78));
        top.GradientStops.Add(new GradientStop(Color.FromArgb(115, 0, 0, 0), 1));
        context.FillRectangle(top, new Rect(Bounds.Size));
    }

    private AtlasNodePresentationViewModel? HitTestNode(Point screenPoint)
    {
        foreach (var node in nodes.Where(node => node.IsCapability))
        {
            if (!IsCapabilityVisible(node) || !nodePlaces.TryGetValue(node.NodeId, out var world))
                continue;
            if (Contains(screenPoint, Screen(world), CapabilityWidth, CapabilityHeight))
                return node;
        }

        foreach (var node in nodes.Where(node => node.IsCore || node.IsService).OrderByDescending(node => node.IsCore))
        {
            if (!nodePlaces.TryGetValue(node.NodeId, out var world))
                continue;
            var width = node.IsCore ? CoreWidth : node.IsSharedCapabilityProvider ? InfrastructureWidth : ProductWidth;
            var height = node.IsCore ? CoreHeight : node.IsSharedCapabilityProvider ? InfrastructureHeight : ProductHeight;
            if (Contains(screenPoint, Screen(world), width, height))
                return node;
        }

        return null;
    }

    private bool IsCapabilityVisible(AtlasNodePresentationViewModel capability)
    {
        if (zoom >= DetailZoom || string.Equals(capability.NodeId, selectedNodeId, StringComparison.Ordinal))
            return true;
        return connections.Any(connection =>
            connection.Kind == AtlasConnectionKind.CapabilityOwnership
            && string.Equals(connection.Target.NodeId, capability.NodeId, StringComparison.Ordinal)
            && string.Equals(connection.Source.NodeId, selectedNodeId, StringComparison.Ordinal));
    }

    private bool Contains(Point point, Point center, double width, double height)
    {
        var halfWidth = width * zoom / 2;
        var halfHeight = height * zoom / 2;
        return point.X >= center.X - halfWidth && point.X <= center.X + halfWidth
            && point.Y >= center.Y - halfHeight && point.Y <= center.Y + halfHeight;
    }

    private bool MoveSelection(Vector direction)
    {
        var current = SelectedNode();
        if (current is null || !nodePlaces.TryGetValue(current.NodeId, out var origin))
        {
            var core = nodes.FirstOrDefault(node => node.IsCore);
            if (core is null)
                return false;
            NodeInvoked?.Invoke(core);
            return true;
        }

        AtlasNodePresentationViewModel? best = null;
        var bestScore = double.MaxValue;
        foreach (var candidate in nodes.Where(node => (node.IsCore || node.IsService) && !string.Equals(node.NodeId, current.NodeId, StringComparison.Ordinal)))
        {
            if (!nodePlaces.TryGetValue(candidate.NodeId, out var target))
                continue;
            var delta = target - origin;
            var primary = delta.X * direction.X + delta.Y * direction.Y;
            if (primary <= 12)
                continue;
            var perpendicular = Math.Abs(delta.X * direction.Y - delta.Y * direction.X);
            var score = primary + perpendicular * 2.4;
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        if (best is null)
            return false;
        NodeInvoked?.Invoke(best);
        return true;
    }

    private AtlasNodePresentationViewModel? SelectedNode() =>
        selectedNodeId is null
            ? null
            : nodes.FirstOrDefault(node => string.Equals(node.NodeId, selectedNodeId, StringComparison.Ordinal));

    private double UpdateInteraction(string nodeId, double target)
    {
        interactionVisuals.TryGetValue(nodeId, out var current);
        if (reducedMotion)
        {
            interactionVisuals[nodeId] = target;
            return target;
        }

        var next = current + (target - current) * 0.28d;
        if (Math.Abs(target - next) < 0.002)
            next = target;
        interactionVisuals[nodeId] = next;
        return next;
    }

    private static Color AccentFor(AtlasNodePresentationViewModel node)
    {
        if (node.IsCore)
            return CoreAccent;
        var serviceId = node.ServiceIdentity?.Value;
        return serviceId switch
        {
            "vocation" => VocationAccent,
            "illumination" => IlluminationAccent,
            "orientation" => OrientationAccent,
            "conveyance" => ConveyanceAccent,
            _ => GenericAccent
        };
    }

    private Point Screen(Point world) => new(
        (WorldCenterX + world.X) * zoom + translateX,
        (WorldCenterY + world.Y) * zoom + translateY);

    private static StreamGeometry RoundedRect(Rect rect, double radius)
    {
        var r = Math.Min(Math.Max(0, radius), Math.Min(rect.Width, rect.Height) / 2);
        var geometry = new StreamGeometry();
        using var gc = geometry.Open();
        gc.BeginFigure(new Point(rect.Left + r, rect.Top), true);
        gc.LineTo(new Point(rect.Right - r, rect.Top), true);
        gc.QuadraticBezierTo(new Point(rect.Right, rect.Top), new Point(rect.Right, rect.Top + r), true);
        gc.LineTo(new Point(rect.Right, rect.Bottom - r), true);
        gc.QuadraticBezierTo(new Point(rect.Right, rect.Bottom), new Point(rect.Right - r, rect.Bottom), true);
        gc.LineTo(new Point(rect.Left + r, rect.Bottom), true);
        gc.QuadraticBezierTo(new Point(rect.Left, rect.Bottom), new Point(rect.Left, rect.Bottom - r), true);
        gc.LineTo(new Point(rect.Left, rect.Top + r), true);
        gc.QuadraticBezierTo(new Point(rect.Left, rect.Top), new Point(rect.Left + r, rect.Top), true);
        gc.EndFigure(true);
        return geometry;
    }

    private static void DrawText(
        DrawingContext context,
        string text,
        Point topLeft,
        double fontSize,
        Color color,
        FontWeight weight)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, weight),
            fontSize,
            new SolidColorBrush(color));
        context.DrawText(formatted, topLeft);
    }

    private static Color Mix(Color from, Color to, double t) => Color.FromArgb(
        (byte)Math.Clamp(Math.Round(from.A + (to.A - from.A) * t), 0, 255),
        (byte)Math.Clamp(Math.Round(from.R + (to.R - from.R) * t), 0, 255),
        (byte)Math.Clamp(Math.Round(from.G + (to.G - from.G) * t), 0, 255),
        (byte)Math.Clamp(Math.Round(from.B + (to.B - from.B) * t), 0, 255));

    private static Color WithAlpha(Color color, byte alpha) => Color.FromArgb(alpha, color.R, color.G, color.B);

    private void RequestSceneFrame()
    {
        if (reducedMotion)
            return;

        var needsFrame = interactionVisuals.Any(pair =>
        {
            var target = string.Equals(pair.Key, pressedNodeId, StringComparison.Ordinal)
                ? -1d
                : string.Equals(pair.Key, hoverNodeId, StringComparison.Ordinal)
                    ? 1d
                    : string.Equals(pair.Key, selectedNodeId, StringComparison.Ordinal)
                        ? 0.52d
                        : 0d;
            return Math.Abs(pair.Value - target) > 0.002;
        });
        if (needsFrame)
            TopLevel.GetTopLevel(this)?.RequestAnimationFrame(_ => InvalidateVisual());
    }
}
