using Avalonia;
using Avalonia.Media;
using WiiiiGotThis.Application;
using AtlasPath = Avalonia.Controls.Shapes.Path;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopAtlasView
{
    private void RebuildFinalConnectionGeometry()
    {
        foreach (var path in SceneCanvas.Children.OfType<AtlasPath>())
        {
            if (!path.Classes.Contains("wgt-atlas-connection") ||
                path.DataContext is not AtlasConnectionPresentationViewModel connection)
            {
                continue;
            }

            path.Data = BuildFinalConnectionGeometry(connection);
            path.Classes.Add("surface-anchored");
        }
    }

    private static StreamGeometry BuildFinalConnectionGeometry(AtlasConnectionPresentationViewModel connection)
    {
        var sourceCenter = WorldPoint(connection.Source);
        var targetCenter = WorldPoint(connection.Target);
        var centerDelta = targetCenter - sourceCenter;
        var centerLength = Math.Sqrt(centerDelta.X * centerDelta.X + centerDelta.Y * centerDelta.Y);
        if (centerLength < 0.001)
            return new StreamGeometry();

        var unit = new Vector(centerDelta.X / centerLength, centerDelta.Y / centerLength);
        var start = sourceCenter + unit * ConnectionAnchorDistance(connection.Source);
        var end = targetCenter - unit * ConnectionAnchorDistance(connection.Target);
        var delta = end - start;
        var length = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);

        var bend = connection.Kind switch
        {
            AtlasConnectionKind.CapabilityDependency => 44d,
            AtlasConnectionKind.CapabilityOwnership => 18d,
            _ => 12d
        };
        var direction = StableCurveDirection(connection.Model.ConnectionId);
        var control = length < 0.001
            ? new Point((start.X + end.X) / 2, (start.Y + end.Y) / 2)
            : new Point(
                (start.X + end.X) / 2 + (-delta.Y / length) * bend * direction,
                (start.Y + end.Y) / 2 + (delta.X / length) * bend * direction);

        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(start, isFilled: false);
        context.QuadraticBezierTo(control, end, isStroked: true);
        context.EndFigure(isClosed: false);

        if (connection.Kind == AtlasConnectionKind.CapabilityDependency)
            AddDependencyArrowHead(context, control, end);

        return geometry;
    }

    private static void AddDependencyArrowHead(StreamGeometryContext context, Point control, Point end)
    {
        var tangent = end - control;
        var length = Math.Sqrt(tangent.X * tangent.X + tangent.Y * tangent.Y);
        if (length < 0.001)
            return;

        var unit = new Vector(tangent.X / length, tangent.Y / length);
        var perpendicular = new Vector(-unit.Y, unit.X);
        const double arrowLength = 10d;
        const double arrowWidth = 4.5d;
        var basePoint = end - unit * arrowLength;
        var left = basePoint + perpendicular * arrowWidth;
        var right = basePoint - perpendicular * arrowWidth;

        context.BeginFigure(left, isFilled: false);
        context.LineTo(end);
        context.LineTo(right);
        context.EndFigure(isClosed: false);
    }

    private static double ConnectionAnchorDistance(AtlasNodePresentationViewModel node) => node.Kind switch
    {
        AtlasNodeKind.Core => 94d,
        AtlasNodeKind.Service => 74d,
        AtlasNodeKind.Capability => 16d,
        _ => 12d
    };
}
