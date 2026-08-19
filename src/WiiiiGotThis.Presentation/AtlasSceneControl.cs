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
/// this control owns only Atlas scene rendering and scene hit testing.
/// </summary>
public sealed class AtlasSceneControl : Control
{
    private const double WorldCenterX = 1000d;
    private const double WorldCenterY = 660d;
    private static readonly TimeSpan ThemeTransitionDuration = TimeSpan.FromMilliseconds(180);

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
        RequestThemeFrame();
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

        context.FillRectangle(new SolidColorBrush(palette.Background), new Rect(Bounds.Size));
        DrawThemeField(context, palette);

        var focused = AtlasPresentationFocus.Build(connections, selectedNodeId);
        foreach (var connection in connections)
            DrawConnection(context, connection, focused, palette);

        foreach (var node in nodes.Where(node => node.IsCapability))
            DrawCapability(context, node, focused, palette);

        foreach (var node in nodes.Where(node => !node.IsCapability))
            DrawProductNode(context, node, focused, palette);

        RequestThemeFrame();
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

    private void DrawThemeField(DrawingContext context, ScenePalette palette)
    {
        var primary = new SolidColorBrush(WithAlpha(palette.Primary, 38));
        var secondary = new SolidColorBrush(WithAlpha(palette.Secondary, 28));
        var center = ToScreen(WorldCenterX, WorldCenterY);

        switch (theme)
        {
            case AtlasThemePreference.Technical:
                context.DrawLine(new Pen(primary, 1), new Point(center.X - 215 * zoom, center.Y), new Point(center.X + 215 * zoom, center.Y));
                context.DrawLine(new Pen(primary, 1), new Point(center.X, center.Y - 185 * zoom), new Point(center.X, center.Y + 185 * zoom));
                DrawCrosshair(context, center, 18 * zoom, primary);
                break;
            case AtlasThemePreference.Elegant:
                context.DrawEllipse(secondary, null, center, 210 * zoom, 150 * zoom);
                context.DrawEllipse(null, new Pen(primary, 1), center, 145 * zoom, 105 * zoom);
                break;
            case AtlasThemePreference.Machine:
                var halfWidth = 250 * zoom;
                var halfHeight = 175 * zoom;
                var frame = new Rect(center.X - halfWidth, center.Y - halfHeight, halfWidth * 2, halfHeight * 2);
                context.DrawRectangle(null, new Pen(primary, 1), frame);
                DrawMachineCorners(context, frame, 26 * zoom, new Pen(new SolidColorBrush(WithAlpha(palette.Primary, 96)), 2));
                break;
            case AtlasThemePreference.World:
                context.DrawEllipse(secondary, null, new Point(center.X - 105 * zoom, center.Y - 45 * zoom), 145 * zoom, 78 * zoom);
                context.DrawEllipse(secondary, null, new Point(center.X + 135 * zoom, center.Y + 72 * zoom), 170 * zoom, 92 * zoom);
                context.DrawEllipse(null, new Pen(primary, 1), center, 225 * zoom, 165 * zoom);
                break;
        }
    }

    private void DrawConnection(
        DrawingContext context,
        AtlasConnectionPresentationViewModel connection,
        IReadOnlySet<string> focused,
        ScenePalette palette)
    {
        var startCenter = ScreenPoint(connection.Source);
        var endCenter = ScreenPoint(connection.Target);
        var delta = endCenter - startCenter;
        var length = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
        if (length < 0.01)
            return;

        var unit = new Vector(delta.X / length, delta.Y / length);
        var start = startCenter + unit * NodeAnchorRadius(connection.Source);
        var end = endCenter - unit * NodeAnchorRadius(connection.Target);
        var span = end - start;
        var spanLength = Math.Sqrt(span.X * span.X + span.Y * span.Y);
        var bend = connection.Kind switch
        {
            AtlasConnectionKind.CapabilityDependency => 34d * zoom,
            AtlasConnectionKind.CapabilityOwnership => 14d * zoom,
            _ => 9d * zoom
        };
        var direction = StableDirection(connection.Model.ConnectionId);
        var control = spanLength < 0.01
            ? new Point((start.X + end.X) / 2, (start.Y + end.Y) / 2)
            : new Point(
                (start.X + end.X) / 2 + (-span.Y / spanLength) * bend * direction,
                (start.Y + end.Y) / 2 + (span.X / spanLength) * bend * direction);

        var focusedConnection = selectedNodeId is not null
            && focused.Contains(connection.Source.NodeId)
            && focused.Contains(connection.Target.NodeId);
        var dimmed = selectedNodeId is not null && !focusedConnection;
        var color = connection.Kind == AtlasConnectionKind.CapabilityDependency
            ? palette.Dependency
            : palette.Edge;
        color = WithAlpha(color, dimmed ? (byte)34 : focusedConnection ? (byte)220 : (byte)102);
        var pen = new Pen(new SolidColorBrush(color), connection.Kind == AtlasConnectionKind.CapabilityDependency ? 1.8 : 1.2);

        var geometry = new StreamGeometry();
        using (var geometryContext = geometry.Open())
        {
            geometryContext.BeginFigure(start, isFilled: false);
            geometryContext.QuadraticBezierTo(control, end, isStroked: true);
            geometryContext.EndFigure(isClosed: false);
        }
        context.DrawGeometry(null, pen, geometry);

        if (connection.Kind == AtlasConnectionKind.CapabilityDependency)
            DrawArrowHead(context, control, end, pen);
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
        var opacity = contextual ? 1d : 0.28d;

        using (context.PushOpacity(opacity))
        {
            var radius = (node.IsCore ? 72d : 54d) * zoom;
            var primary = new SolidColorBrush(WithAlpha(palette.Primary, selected ? (byte)255 : (byte)205));
            var faint = new SolidColorBrush(WithAlpha(palette.Primary, selected ? (byte)52 : (byte)22));
            var outline = new Pen(primary, selected ? 2.2 : 1.2);

            switch (theme)
            {
                case AtlasThemePreference.Technical:
                    context.DrawEllipse(faint, outline, center, radius, radius);
                    DrawCrosshair(context, center, radius + 9 * zoom, new SolidColorBrush(WithAlpha(palette.Primary, 118)));
                    break;
                case AtlasThemePreference.Elegant:
                    context.DrawEllipse(faint, null, center, radius + 10 * zoom, radius + 10 * zoom);
                    context.DrawEllipse(null, outline, center, radius, radius);
                    context.DrawEllipse(null, new Pen(new SolidColorBrush(WithAlpha(palette.Secondary, 80)), 1), center, radius - 8 * zoom, radius - 8 * zoom);
                    break;
                case AtlasThemePreference.Machine:
                    var box = new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2);
                    context.FillRectangle(faint, box);
                    context.DrawRectangle(null, outline, box);
                    DrawMachineCorners(context, box, 13 * zoom, new Pen(primary, selected ? 2.6 : 1.6));
                    break;
                case AtlasThemePreference.World:
                    context.DrawEllipse(faint, outline, center, radius, radius * 0.9);
                    context.DrawEllipse(new SolidColorBrush(WithAlpha(palette.Secondary, 28)), null,
                        new Point(center.X, center.Y + radius * 0.42), radius * 0.82, radius * 0.34);
                    break;
            }

            DrawNodeSigil(context, node, center, radius * 0.48, new Pen(primary, node.IsCore ? 2.2 : 1.8));
            DrawNodeStatus(context, node, center, radius, palette);
            DrawCenteredText(context, node.IsCore ? "Wiiii Got This" : node.Title,
                new Point(center.X, center.Y + radius + 9 * zoom),
                Math.Clamp((node.IsCore ? 13d : 11.5d) * zoom, 9d, 15d),
                palette.Text);
            DrawCenteredText(context, node.CompactStateText,
                new Point(center.X, center.Y + radius + 25 * zoom),
                Math.Clamp(8d * zoom, 7d, 10d),
                palette.Muted);
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
        var radius = Math.Max(4d, 6d * zoom);
        var color = node.IsAvailable ? palette.Available : palette.Unavailable;

        using (context.PushOpacity(contextual ? 0.92 : 0.18))
        {
            context.DrawEllipse(new SolidColorBrush(WithAlpha(color, selected ? (byte)255 : (byte)210)),
                selected ? new Pen(new SolidColorBrush(palette.Text), 1) : null,
                center,
                selected ? radius + 2 : radius,
                selected ? radius + 2 : radius);

            if (expanded)
            {
                var labelPoint = new Point(center.X + (node.X >= 0 ? 12 : -12) * zoom, center.Y - 6 * zoom);
                DrawAnchoredText(context, node.Title, labelPoint, node.X >= 0,
                    Math.Clamp(9.5d * zoom, 8d, 11d), palette.Text);
            }
        }
    }

    private void DrawNodeSigil(DrawingContext context, AtlasNodePresentationViewModel node, Point center, double radius, Pen pen)
    {
        if (node.IsCore)
        {
            context.DrawEllipse(null, pen, center, radius * 0.62, radius * 0.62);
            context.DrawLine(pen, new Point(center.X - radius, center.Y), new Point(center.X - radius * 0.42, center.Y));
            context.DrawLine(pen, new Point(center.X + radius * 0.42, center.Y), new Point(center.X + radius, center.Y));
            context.DrawLine(pen, new Point(center.X, center.Y - radius), new Point(center.X, center.Y - radius * 0.42));
            context.DrawLine(pen, new Point(center.X, center.Y + radius * 0.42), new Point(center.X, center.Y + radius));
            return;
        }

        switch (node.ServiceIdentity?.Value)
        {
            case "vocation":
                context.DrawLine(pen, new Point(center.X - radius * 0.7, center.Y - radius * 0.55), new Point(center.X, center.Y + radius * 0.65));
                context.DrawLine(pen, new Point(center.X, center.Y + radius * 0.65), new Point(center.X + radius * 0.7, center.Y - radius * 0.55));
                break;
            case "illumination":
                context.DrawEllipse(null, pen, center, radius * 0.34, radius * 0.34);
                for (var index = 0; index < 8; index++)
                {
                    var angle = Math.PI * 2 * index / 8d;
                    var inner = new Point(center.X + Math.Cos(angle) * radius * 0.52, center.Y + Math.Sin(angle) * radius * 0.52);
                    var outer = new Point(center.X + Math.Cos(angle) * radius * 0.9, center.Y + Math.Sin(angle) * radius * 0.9);
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
                context.DrawLine(pen, new Point(center.X - radius * 0.72, center.Y - radius * 0.35), new Point(center.X + radius * 0.72, center.Y - radius * 0.35));
                context.DrawLine(pen, new Point(center.X - radius * 0.72, center.Y + radius * 0.35), new Point(center.X + radius * 0.72, center.Y + radius * 0.35));
                context.DrawLine(pen, new Point(center.X + radius * 0.3, center.Y - radius * 0.65), new Point(center.X + radius * 0.72, center.Y - radius * 0.35));
                context.DrawLine(pen, new Point(center.X + radius * 0.3, center.Y + radius * 0.05), new Point(center.X + radius * 0.72, center.Y + radius * 0.35));
                break;
            default:
                context.DrawEllipse(null, pen, center, radius * 0.62, radius * 0.62);
                context.DrawLine(pen, new Point(center.X - radius * 0.72, center.Y), new Point(center.X + radius * 0.72, center.Y));
                break;
        }
    }

    private void DrawNodeStatus(DrawingContext context, AtlasNodePresentationViewModel node, Point center, double radius, ScenePalette palette)
    {
        if (node.IsCore)
            return;
        var statusColor = node.IsAvailable ? palette.Available : palette.Unavailable;
        var statusCenter = new Point(center.X + radius * 0.72, center.Y - radius * 0.72);
        context.DrawEllipse(new SolidColorBrush(statusColor), null, statusCenter, 3.5, 3.5);
    }

    private AtlasNodePresentationViewModel? HitTestNode(Point screenPoint)
    {
        var focused = AtlasPresentationFocus.Build(connections, selectedNodeId);
        foreach (var node in nodes.OrderByDescending(node => node.IsCapability ? 1 : 2))
        {
            var center = ScreenPoint(node);
            var radius = node.Kind switch
            {
                AtlasNodeKind.Core => Math.Max(44d, 78d * zoom),
                AtlasNodeKind.Service => Math.Max(38d, 60d * zoom),
                AtlasNodeKind.Capability when selectedNodeId is not null && focused.Contains(node.NodeId) => Math.Max(18d, 24d * zoom),
                _ => Math.Max(12d, 16d * zoom)
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

    private double NodeAnchorRadius(AtlasNodePresentationViewModel node) => node.Kind switch
    {
        AtlasNodeKind.Core => 72d * zoom,
        AtlasNodeKind.Service => 54d * zoom,
        AtlasNodeKind.Capability => Math.Max(5d, 6d * zoom),
        _ => 8d * zoom
    };

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
        var x = extendRight ? anchor.X : anchor.X - formatted.Width;
        context.DrawText(formatted, new Point(x, anchor.Y));
    }

    private static void DrawCrosshair(DrawingContext context, Point center, double radius, IBrush brush)
    {
        var pen = new Pen(brush, 1);
        const double tick = 7d;
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
        var t = Math.Clamp(elapsed.TotalMilliseconds / ThemeTransitionDuration.TotalMilliseconds, 0d, 1d);
        return 1d - Math.Pow(1d - t, 3d);
    }

    private void RequestThemeFrame()
    {
        if (!themeTransitionActive || reducedMotion)
            return;
        TopLevel.GetTopLevel(this)?.RequestAnimationFrame(_ => InvalidateVisual());
    }

    private static double StableDirection(string value)
    {
        var checksum = 0;
        foreach (var character in value)
            checksum = (checksum + character) % 2;
        return checksum == 0 ? 1d : -1d;
    }

    private static Color WithAlpha(Color color, byte alpha) =>
        Color.FromArgb(alpha, color.R, color.G, color.B);

    private readonly record struct ScenePalette(
        Color Background,
        Color Primary,
        Color Secondary,
        Color Edge,
        Color Dependency,
        Color Text,
        Color Muted,
        Color Available,
        Color Unavailable)
    {
        public static ScenePalette For(AtlasThemePreference theme) => theme switch
        {
            AtlasThemePreference.Elegant => new(
                Color.Parse("#151216"), Color.Parse("#D9C0A7"), Color.Parse("#745F6B"),
                Color.Parse("#806D75"), Color.Parse("#D8A9A9"), Color.Parse("#F1E7DE"),
                Color.Parse("#A89691"), Color.Parse("#9DD3B3"), Color.Parse("#B27979")),
            AtlasThemePreference.Machine => new(
                Color.Parse("#03100A"), Color.Parse("#70F1AE"), Color.Parse("#276F4D"),
                Color.Parse("#2D8E61"), Color.Parse("#B7F17B"), Color.Parse("#DBFFE9"),
                Color.Parse("#76AA8C"), Color.Parse("#73F0AD"), Color.Parse("#D77474")),
            AtlasThemePreference.World => new(
                Color.Parse("#07140D"), Color.Parse("#8BD5A9"), Color.Parse("#315F46"),
                Color.Parse("#47775A"), Color.Parse("#C5D985"), Color.Parse("#E5F4E9"),
                Color.Parse("#85A68F"), Color.Parse("#91DEB4"), Color.Parse("#CF7777")),
            _ => new(
                Color.Parse("#061118"), Color.Parse("#67DCF4"), Color.Parse("#245D72"),
                Color.Parse("#337E96"), Color.Parse("#A8C9FF"), Color.Parse("#E2F9FF"),
                Color.Parse("#789CA7"), Color.Parse("#6CE0C1"), Color.Parse("#D66F7C"))
        };

        public static ScenePalette Lerp(ScenePalette from, ScenePalette to, double t) => new(
            Mix(from.Background, to.Background, t),
            Mix(from.Primary, to.Primary, t),
            Mix(from.Secondary, to.Secondary, t),
            Mix(from.Edge, to.Edge, t),
            Mix(from.Dependency, to.Dependency, t),
            Mix(from.Text, to.Text, t),
            Mix(from.Muted, to.Muted, t),
            Mix(from.Available, to.Available, t),
            Mix(from.Unavailable, to.Unavailable, t));

        private static Color Mix(Color from, Color to, double t) => Color.FromArgb(
            LerpByte(from.A, to.A, t),
            LerpByte(from.R, to.R, t),
            LerpByte(from.G, to.G, t),
            LerpByte(from.B, to.B, t));

        private static byte LerpByte(byte from, byte to, double t) =>
            (byte)Math.Clamp(Math.Round(from + (to - from) * t), byte.MinValue, byte.MaxValue);
    }
}