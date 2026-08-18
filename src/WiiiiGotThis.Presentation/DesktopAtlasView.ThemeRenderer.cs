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
            ClipToBounds = false
        };
        themeAmbientLayer.Classes.Add("wgt-theme-ambient-layer");

        AddAmbientRing("wgt-theme-technical-reticle", 316, 316, 532, 282, 158);
        AddAmbientRing("wgt-theme-technical-reticle", 520, 520, 430, 180, 260);

        AddAmbientRail("wgt-theme-machine-rail", 760, 1, 310, 176);
        AddAmbientRail("wgt-theme-machine-rail", 760, 1, 310, 704);
        AddAmbientRail("wgt-theme-machine-rail", 1, 484, 250, 198);
        AddAmbientRail("wgt-theme-machine-rail", 1, 484, 1129, 198);
        AddAmbientCorner("wgt-theme-machine-corner", 250, 176, 1, 1);
        AddAmbientCorner("wgt-theme-machine-corner", 1074, 176, -1, 1);
        AddAmbientCorner("wgt-theme-machine-corner", 250, 648, 1, -1);
        AddAmbientCorner("wgt-theme-machine-corner", 1074, 648, -1, -1);

        AddAmbientRing("wgt-theme-world-orbit", 640, 640, 370, 120, 320);
        AddAmbientRing("wgt-theme-world-orbit", 970, 610, 205, 136, 305);
        AddAmbientRing("wgt-theme-world-orbit", 420, 300, 710, 510, 150);
        AddAmbientDot("wgt-theme-world-beacon", 8, 8, 406, 196);
        AddAmbientDot("wgt-theme-world-beacon", 6, 6, 1014, 222);
        AddAmbientDot("wgt-theme-world-beacon", 7, 7, 1048, 646);
        AddAmbientDot("wgt-theme-world-beacon", 5, 5, 333, 670);

        AddAmbientRing("wgt-theme-elegant-halo", 760, 500, 310, 190, 250);
        AddAmbientRing("wgt-theme-elegant-halo", 390, 390, 710, 250, 195);

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
        const double arm = 54;
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
