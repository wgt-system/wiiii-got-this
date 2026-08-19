using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace WiiiiGotThis.Presentation;

/// <summary>
/// Authored environmental structure for the World renderer.
///
/// The base living-world renderer owns settlements and primary roads. This overlay adds
/// quieter landscape cues that make the overview read as one place rather than four radial
/// UI islands: farmland, drainage, rail, parkland and topographic traces. It has no product
/// or capability authority and intentionally stays non-interactive.
/// </summary>
public sealed class AtlasWorldEnvironmentalDetailOverlay : Control
{
    private const double WorldCenterX = 1000d;
    private const double WorldCenterY = 660d;

    private static readonly Color Field = Color.Parse("#263920");
    private static readonly Color FieldEdge = Color.Parse("#50633B");
    private static readonly Color Grass = Color.Parse("#153326");
    private static readonly Color Park = Color.Parse("#174332");
    private static readonly Color Stream = Color.Parse("#174B49");
    private static readonly Color StreamLight = Color.Parse("#28746F");
    private static readonly Color Rail = Color.Parse("#45524D");
    private static readonly Color RailLight = Color.Parse("#7A8D84");
    private static readonly Color Contour = Color.Parse("#3F6756");
    private static readonly Color Soil = Color.Parse("#473D2D");
    private static readonly Color Orchard = Color.Parse("#315136");
    private static readonly Color Footpath = Color.Parse("#7E745A");
    private static readonly Color Garden = Color.Parse("#28493A");

    private double zoom = 0.82d;
    private double translateX;
    private double translateY;

    public AtlasWorldEnvironmentalDetailOverlay()
    {
        IsHitTestVisible = false;
        ClipToBounds = true;
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

        DrawVocationFarmland(context);
        DrawVocationOrchard(context);
        DrawIlluminationTerraces(context);
        DrawIlluminationGarden(context);
        DrawWgtGreenBelt(context);
        DrawOrientationContours(context);
        DrawOrientationTrail(context);
        DrawTributary(context);
        DrawConveyanceRail(context);
        DrawAuthoredTreeMasses(context);
        DrawMinorLandscapeBoundaries(context);
    }

    private void DrawVocationFarmland(DrawingContext context)
    {
        var fieldA = new[]
        {
            new Point(-690, 38), new Point(-585, -8), new Point(-492, 24),
            new Point(-516, 112), new Point(-628, 134), new Point(-708, 96)
        };
        var fieldB = new[]
        {
            new Point(-648, 160), new Point(-525, 126), new Point(-452, 166),
            new Point(-493, 244), new Point(-610, 252), new Point(-680, 214)
        };
        DrawField(context, fieldA, 0.70);
        DrawField(context, fieldB, 0.52);
    }

    private void DrawVocationOrchard(DrawingContext context)
    {
        if (zoom < 0.54)
            return;

        var treeBrush = new SolidColorBrush(WithAlpha(Orchard, 126));
        var trunkPen = new Pen(new SolidColorBrush(WithAlpha(Soil, 92)), Math.Max(0.5, 0.65 * zoom));
        var origin = new Point(-742, 182);
        for (var row = 0; row < 4; row++)
        {
            for (var column = 0; column < 5; column++)
            {
                var stagger = row % 2 == 0 ? 0d : 7d;
                var world = new Point(origin.X + column * 27 + stagger, origin.Y + row * 23);
                var screen = ScreenPoint(world);
                var crown = Math.Max(2.1, 4.4 * zoom);
                context.DrawLine(
                    trunkPen,
                    ScreenPoint(world + new Vector(0, 3.5)),
                    ScreenPoint(world + new Vector(0, 7.5)));
                context.DrawEllipse(treeBrush, null, screen, crown, crown * 0.82);
            }
        }
    }

    private void DrawIlluminationTerraces(DrawingContext context)
    {
        var terrace = new[]
        {
            new Point(-332, -530), new Point(-210, -586), new Point(-62, -566),
            new Point(-86, -502), new Point(-228, -476), new Point(-342, -486)
        };
        DrawField(context, terrace, 0.34);

        var pen = new Pen(new SolidColorBrush(WithAlpha(FieldEdge, 46)), Math.Max(0.55, 0.72 * zoom));
        for (var index = 0; index < 4; index++)
        {
            var y = -538 + index * 18;
            var start = new Point(-292 + index * 8, y);
            var end = new Point(-104 - index * 6, y + 8);
            context.DrawLine(pen, ScreenPoint(start), ScreenPoint(end));
        }
    }

    private void DrawIlluminationGarden(DrawingContext context)
    {
        var garden = new[]
        {
            new Point(-374, -420), new Point(-312, -454), new Point(-240, -438),
            new Point(-210, -382), new Point(-256, -342), new Point(-332, -350),
            new Point(-388, -382)
        };
        context.DrawGeometry(
            new SolidColorBrush(WithAlpha(Garden, 88)),
            new Pen(new SolidColorBrush(WithAlpha(FieldEdge, 36)), Math.Max(0.5, 0.68 * zoom)),
            PolygonWorld(garden));

        if (zoom < 0.62)
            return;

        var pathPen = new Pen(new SolidColorBrush(WithAlpha(Footpath, 72)), Math.Max(0.85, 1.15 * zoom));
        context.DrawGeometry(
            null,
            pathPen,
            SmoothWorldPath(
            [
                new Point(-365, -395), new Point(-326, -414), new Point(-286, -401),
                new Point(-253, -371), new Point(-276, -354)
            ]));
        context.DrawGeometry(
            null,
            pathPen,
            SmoothWorldPath(
            [
                new Point(-328, -445), new Point(-319, -411), new Point(-331, -376), new Point(-356, -358)
            ]));
    }

    private void DrawWgtGreenBelt(DrawingContext context)
    {
        var park = new[]
        {
            new Point(-182, 146), new Point(-110, 122), new Point(-34, 138),
            new Point(4, 188), new Point(-45, 224), new Point(-128, 218), new Point(-192, 188)
        };
        context.DrawGeometry(
            new SolidColorBrush(WithAlpha(Park, 122)),
            new Pen(new SolidColorBrush(WithAlpha(Contour, 54)), Math.Max(0.55, 0.75 * zoom)),
            PolygonWorld(park));

        var pathPen = new Pen(new SolidColorBrush(WithAlpha(FieldEdge, 42)), Math.Max(1.1, 1.6 * zoom));
        context.DrawLine(pathPen, ScreenPoint(new Point(-154, 185)), ScreenPoint(new Point(-31, 171)));
        context.DrawLine(pathPen, ScreenPoint(new Point(-102, 135)), ScreenPoint(new Point(-92, 215)));

        if (zoom < 0.72)
            return;

        var promenadePen = new Pen(new SolidColorBrush(WithAlpha(Footpath, 58)), Math.Max(0.8, 1.05 * zoom));
        context.DrawGeometry(
            null,
            promenadePen,
            SmoothWorldPath(
            [
                new Point(-28, 86), new Point(18, 112), new Point(42, 151),
                new Point(36, 202), new Point(8, 235)
            ]));
    }

    private void DrawOrientationContours(DrawingContext context)
    {
        var pen = new Pen(new SolidColorBrush(WithAlpha(Contour, 46)), Math.Max(0.55, 0.8 * zoom));
        var contours = new[]
        {
            new[] { new Point(500, -196), new Point(565, -224), new Point(645, -216), new Point(702, -174) },
            new[] { new Point(522, -168), new Point(580, -190), new Point(645, -184), new Point(686, -151) },
            new[] { new Point(544, -141), new Point(597, -158), new Point(650, -151), new Point(676, -127) }
        };

        foreach (var contour in contours)
            context.DrawGeometry(null, pen, SmoothWorldPath(contour));
    }

    private void DrawOrientationTrail(DrawingContext context)
    {
        if (zoom < 0.58)
            return;

        var trail = new[]
        {
            new Point(462, -276), new Point(514, -240), new Point(568, -254),
            new Point(622, -226), new Point(676, -237), new Point(726, -199)
        };
        var pen = new Pen(new SolidColorBrush(WithAlpha(Footpath, 76)), Math.Max(0.65, 0.95 * zoom));
        context.DrawGeometry(null, pen, SmoothWorldPath(trail));

        var markerBrush = new SolidColorBrush(WithAlpha(StreamLight, 86));
        for (var index = 1; index < trail.Length - 1; index += 2)
        {
            var marker = ScreenPoint(trail[index]);
            var radius = Math.Max(1.3, 2.2 * zoom);
            context.DrawEllipse(markerBrush, null, marker, radius, radius);
        }
    }

    private void DrawTributary(DrawingContext context)
    {
        var points = new[]
        {
            new Point(-252, -560), new Point(-218, -430), new Point(-154, -304),
            new Point(-126, -165), new Point(-72, -22), new Point(-56, 116), new Point(-38, 230)
        };
        var geometry = SmoothWorldPath(points);
        context.DrawGeometry(
            null,
            new Pen(new SolidColorBrush(WithAlpha(Stream, 112)), Math.Max(3.2, 5.2 * zoom)),
            geometry);
        context.DrawGeometry(
            null,
            new Pen(new SolidColorBrush(WithAlpha(StreamLight, 45)), Math.Max(0.55, 0.75 * zoom)),
            geometry);
    }

    private void DrawConveyanceRail(DrawingContext context)
    {
        var start = new Point(118, 170);
        var control = new Point(196, 272);
        var end = new Point(225, 356);
        var left = OffsetQuadratic(start, control, end, -4.4);
        var right = OffsetQuadratic(start, control, end, 4.4);
        var railPen = new Pen(new SolidColorBrush(WithAlpha(Rail, 165)), Math.Max(0.85, 1.15 * zoom));
        context.DrawGeometry(null, railPen, SmoothWorldPath(left));
        context.DrawGeometry(null, railPen, SmoothWorldPath(right));

        var sleeperPen = new Pen(new SolidColorBrush(WithAlpha(RailLight, 72)), Math.Max(0.65, 0.8 * zoom));
        for (var index = 1; index < 12; index++)
        {
            var t = index / 12d;
            var point = QuadraticPoint(start, control, end, t);
            var next = QuadraticPoint(start, control, end, Math.Min(1, t + 0.02));
            var tangent = Normalize(next - point);
            var normal = new Vector(-tangent.Y, tangent.X);
            context.DrawLine(
                sleeperPen,
                ScreenPoint(point - normal * 6),
                ScreenPoint(point + normal * 6));
        }
    }

    private void DrawAuthoredTreeMasses(DrawingContext context)
    {
        if (zoom < 0.48)
            return;

        var clusters = new[]
        {
            new[] { new Point(-405, 70), new Point(-375, 92), new Point(-348, 70), new Point(-324, 104), new Point(-292, 91) },
            new[] { new Point(222, -92), new Point(250, -118), new Point(279, -94), new Point(305, -126), new Point(335, -104) },
            new[] { new Point(398, 126), new Point(426, 108), new Point(452, 132), new Point(482, 113) },
            new[] { new Point(-70, -338), new Point(-42, -362), new Point(-8, -349), new Point(18, -380) }
        };

        var brush = new SolidColorBrush(WithAlpha(Grass, 148));
        foreach (var cluster in clusters)
        {
            for (var index = 0; index < cluster.Length; index++)
            {
                var point = ScreenPoint(cluster[index]);
                var radius = Math.Max(1.8, (3.5 + index % 2) * zoom);
                context.DrawEllipse(brush, null, point, radius, radius * 0.86);
            }
        }
    }

    private void DrawMinorLandscapeBoundaries(DrawingContext context)
    {
        var pen = new Pen(new SolidColorBrush(WithAlpha(FieldEdge, 28)), Math.Max(0.5, 0.7 * zoom));
        var boundaries = new[]
        {
            new[] { new Point(-760, -38), new Point(-640, -78), new Point(-525, -74), new Point(-408, -104) },
            new[] { new Point(236, 198), new Point(330, 155), new Point(448, 162), new Point(558, 128) },
            new[] { new Point(102, -262), new Point(220, -286), new Point(336, -250), new Point(440, -278) }
        };
        foreach (var boundary in boundaries)
            context.DrawGeometry(null, pen, SmoothWorldPath(boundary));
    }

    private void DrawField(DrawingContext context, Point[] points, double warmMix)
    {
        var fill = Mix(Grass, Field, warmMix);
        var geometry = PolygonWorld(points);
        context.DrawGeometry(
            new SolidColorBrush(WithAlpha(fill, 94)),
            new Pen(new SolidColorBrush(WithAlpha(FieldEdge, 42)), Math.Max(0.55, 0.72 * zoom)),
            geometry);

        if (zoom < 0.64)
            return;

        var bounds = BoundsOf(points);
        var rowPen = new Pen(new SolidColorBrush(WithAlpha(Mix(FieldEdge, Soil, 0.42), 35)), Math.Max(0.45, 0.55 * zoom));
        for (var index = 1; index <= 5; index++)
        {
            var fraction = index / 6d;
            var y = bounds.Top + bounds.Height * fraction;
            context.DrawLine(
                rowPen,
                ScreenPoint(new Point(bounds.Left + 18, y)),
                ScreenPoint(new Point(bounds.Right - 18, y + 5)));
        }
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

    private StreamGeometry SmoothWorldPath(Point[] points)
    {
        var geometry = new StreamGeometry();
        using var gc = geometry.Open();
        if (points.Length == 0)
            return geometry;
        gc.BeginFigure(ScreenPoint(points[0]), isFilled: false);
        if (points.Length == 1)
        {
            gc.EndFigure(isClosed: false);
            return geometry;
        }
        for (var index = 1; index < points.Length - 1; index++)
        {
            var current = ScreenPoint(points[index]);
            var next = ScreenPoint(points[index + 1]);
            gc.QuadraticBezierTo(current, MidPoint(current, next), isStroked: true);
        }
        gc.LineTo(ScreenPoint(points[^1]), isStroked: true);
        gc.EndFigure(isClosed: false);
        return geometry;
    }

    private static Point[] OffsetQuadratic(Point start, Point control, Point end, double offset)
    {
        var points = new Point[13];
        for (var index = 0; index < points.Length; index++)
        {
            var t = index / (double)(points.Length - 1);
            var point = QuadraticPoint(start, control, end, t);
            var ahead = QuadraticPoint(start, control, end, Math.Min(1, t + 0.015));
            var tangent = Normalize(ahead - point);
            var normal = new Vector(-tangent.Y, tangent.X);
            points[index] = point + normal * offset;
        }
        return points;
    }

    private static Point QuadraticPoint(Point start, Point control, Point end, double t)
    {
        var inverse = 1 - t;
        return new Point(
            inverse * inverse * start.X + 2 * inverse * t * control.X + t * t * end.X,
            inverse * inverse * start.Y + 2 * inverse * t * control.Y + t * t * end.Y);
    }

    private Point ScreenPoint(Point worldPoint) => new(
        (WorldCenterX + worldPoint.X) * zoom + translateX,
        (WorldCenterY + worldPoint.Y) * zoom + translateY);

    private static Vector Normalize(Vector vector)
    {
        var length = Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
        return length < 0.001 ? new Vector(0, -1) : new Vector(vector.X / length, vector.Y / length);
    }

    private static Point MidPoint(Point first, Point second) =>
        new((first.X + second.X) / 2, (first.Y + second.Y) / 2);

    private static Rect BoundsOf(Point[] points)
    {
        var minX = points.Min(point => point.X);
        var maxX = points.Max(point => point.X);
        var minY = points.Min(point => point.Y);
        var maxY = points.Max(point => point.Y);
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    private static Color Mix(Color from, Color to, double t) => Color.FromArgb(
        (byte)Math.Round(from.A + (to.A - from.A) * t),
        (byte)Math.Round(from.R + (to.R - from.R) * t),
        (byte)Math.Round(from.G + (to.G - from.G) * t),
        (byte)Math.Round(from.B + (to.B - from.B) * t));

    private static Color WithAlpha(Color color, byte alpha) =>
        Color.FromArgb(alpha, color.R, color.G, color.B);
}
