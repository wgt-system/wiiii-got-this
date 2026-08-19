using Avalonia;
using Avalonia.Controls;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopAtlasView
{
    private Canvas? themeAmbientLayer;
    private readonly List<StyledElement> themeAmbientElements = [];

    private void EnsureThemeRenderer()
    {
        if (themeAmbientLayer is not null)
            return;

        themeAmbientLayer = new Canvas
        {
            Width = 1380,
            Height = 880,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            IsHitTestVisible = false,
            ClipToBounds = true
        };
        themeAmbientLayer.Classes.Add("wgt-theme-ambient-layer");

        // Technical: compact instrumentation around the visual centre, not giant rings
        // spanning the entire viewport.
        AddAmbientRing("wgt-theme-technical-reticle", 220, 220, 580, 330, 110);
        AddAmbientRing("wgt-theme-technical-reticle", 360, 360, 510, 260, 180);
        AddAmbientRail("wgt-theme-technical-reticle-line", 420, 1, 480, 439);
        AddAmbientRail("wgt-theme-technical-reticle-line", 1, 420, 689, 230);

        // Machine: a bounded engineering frame. It intentionally does not hug the
        // window edges, which previously made the whole UI look boxed in.
        AddAmbientRail("wgt-theme-machine-rail", 560, 1, 410, 270);
        AddAmbientRail("wgt-theme-machine-rail", 560, 1, 410, 610);
        AddAmbientRail("wgt-theme-machine-rail", 1, 340, 410, 270);
        AddAmbientRail("wgt-theme-machine-rail", 1, 340, 970, 270);
        AddAmbientCorner("wgt-theme-machine-corner", 410, 270, 1, 1);
        AddAmbientCorner("wgt-theme-machine-corner", 970, 270, -1, 1);
        AddAmbientCorner("wgt-theme-machine-corner", 410, 610, 1, -1);
        AddAmbientCorner("wgt-theme-machine-corner", 970, 610, -1, -1);

        // World: sparse spatial beacons only. The previous three enormous orbital
        // ellipses dominated the actual service graph and read as unfinished guides.
        AddAmbientDot("wgt-theme-world-beacon", 8, 8, 434, 244);
        AddAmbientDot("wgt-theme-world-beacon", 6, 6, 1012, 278);
        AddAmbientDot("wgt-theme-world-beacon", 7, 7, 1032, 642);
        AddAmbientDot("wgt-theme-world-beacon", 5, 5, 366, 662);
        AddAmbientDot("wgt-theme-world-beacon", 4, 4, 544, 184);
        AddAmbientDot("wgt-theme-world-beacon", 4, 4, 842, 706);

        // Elegant: one restrained halo pair, deliberately quieter than the graph.
        AddAmbientRing("wgt-theme-elegant-halo", 280, 280, 550, 300, 140);
        AddAmbientRing("wgt-theme-elegant-halo", 430, 430, 475, 225, 215);

        AtlasViewport.Children.Insert(0, themeAmbientLayer);
        ApplyThemeRenderer(polishShell?.AtlasTheme ?? visualTheme);
    }

    private void ApplyThemeRenderer(AtlasThemePreference theme)
    {
        EnsureThemeRenderer();
        foreach (var element in themeAmbientElements)
            SetExplicitThemeClass(element, theme);
    }

    private static void SetExplicitThemeClass(StyledElement element, AtlasThemePreference theme)
    {
        foreach (var themeClass in ThemeClasses)
            element.Classes.Remove(themeClass);

        element.Classes.Add(theme switch
        {
            AtlasThemePreference.Technical => "theme-technical",
            AtlasThemePreference.Elegant => "theme-elegant",
            AtlasThemePreference.Machine => "theme-machine",
            AtlasThemePreference.World => "theme-world",
            _ => "theme-technical"
        });
    }

    private void AddAmbientRing(string className, double width, double height, double left, double top, double radius)
    {
        var ring = new Border
        {
            Width = width,
            Height = height,
            CornerRadius = new CornerRadius(radius),
            BorderThickness = new Thickness(1),
            IsHitTestVisible = false
        };
        ring.Classes.Add("wgt-theme-ambient");
        ring.Classes.Add(className);
        Canvas.SetLeft(ring, left);
        Canvas.SetTop(ring, top);
        themeAmbientLayer!.Children.Add(ring);
        themeAmbientElements.Add(ring);
    }

    private void AddAmbientRail(string className, double width, double height, double left, double top)
    {
        var rail = new Border
        {
            Width = width,
            Height = height,
            IsHitTestVisible = false
        };
        rail.Classes.Add("wgt-theme-ambient");
        rail.Classes.Add(className);
        Canvas.SetLeft(rail, left);
        Canvas.SetTop(rail, top);
        themeAmbientLayer!.Children.Add(rail);
        themeAmbientElements.Add(rail);
    }

    private void AddAmbientCorner(string className, double left, double top, double directionX, double directionY)
    {
        const double arm = 42;
        const double thickness = 2;
        AddAmbientRail(className, arm, thickness, directionX > 0 ? left : left - arm, top);
        AddAmbientRail(className, thickness, arm, left, directionY > 0 ? top : top - arm);
    }

    private void AddAmbientDot(string className, double width, double height, double left, double top)
    {
        var dot = new Border
        {
            Width = width,
            Height = height,
            CornerRadius = new CornerRadius(Math.Max(width, height) / 2),
            IsHitTestVisible = false
        };
        dot.Classes.Add("wgt-theme-ambient");
        dot.Classes.Add(className);
        Canvas.SetLeft(dot, left);
        Canvas.SetTop(dot, top);
        themeAmbientLayer!.Children.Add(dot);
        themeAmbientElements.Add(dot);
    }
}
