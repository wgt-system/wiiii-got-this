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

        // A soft field behind WGT Core gives the composition a centre of gravity
        // without another visible ring around the hub.
        AddAmbientField("wgt-theme-core-field", 310, 250, 535, 315, 125);

        // Technical: compact instrument marks around the centre. These read as
        // measurement/navigation cues rather than editor guides across the viewport.
        AddAmbientRail("wgt-theme-technical-axis", 360, 1, 510, 439);
        AddAmbientRail("wgt-theme-technical-axis", 1, 300, 689, 290);
        AddAmbientRail("wgt-theme-technical-tick", 24, 2, 498, 438);
        AddAmbientRail("wgt-theme-technical-tick", 24, 2, 858, 438);
        AddAmbientRail("wgt-theme-technical-tick", 2, 24, 688, 278);
        AddAmbientRail("wgt-theme-technical-tick", 2, 24, 688, 578);

        // Elegant: atmospheric material only. No construction lines.
        AddAmbientField("wgt-theme-elegant-field", 330, 210, 365, 205, 105);
        AddAmbientField("wgt-theme-elegant-field", 280, 190, 795, 505, 95);

        // Machine: a bounded engineering frame around the actual graph, not the
        // application window. Corners/rails are intentionally broken into segments.
        AddAmbientRail("wgt-theme-machine-rail", 210, 1, 430, 270);
        AddAmbientRail("wgt-theme-machine-rail", 210, 1, 740, 270);
        AddAmbientRail("wgt-theme-machine-rail", 210, 1, 430, 610);
        AddAmbientRail("wgt-theme-machine-rail", 210, 1, 740, 610);
        AddAmbientRail("wgt-theme-machine-rail", 1, 112, 410, 292);
        AddAmbientRail("wgt-theme-machine-rail", 1, 112, 410, 476);
        AddAmbientRail("wgt-theme-machine-rail", 1, 112, 970, 292);
        AddAmbientRail("wgt-theme-machine-rail", 1, 112, 970, 476);
        AddAmbientCorner("wgt-theme-machine-corner", 410, 270, 1, 1);
        AddAmbientCorner("wgt-theme-machine-corner", 970, 270, -1, 1);
        AddAmbientCorner("wgt-theme-machine-corner", 410, 610, 1, -1);
        AddAmbientCorner("wgt-theme-machine-corner", 970, 610, -1, -1);

        // World: sparse locations and low-contrast terrain fields. No orbital
        // ellipses; the service graph remains the visual hierarchy.
        AddAmbientField("wgt-theme-world-field", 210, 135, 320, 250, 68);
        AddAmbientField("wgt-theme-world-field", 250, 150, 850, 520, 75);
        AddAmbientField("wgt-theme-world-field", 170, 120, 845, 175, 60);
        AddAmbientDot("wgt-theme-world-beacon", 7, 7, 404, 222);
        AddAmbientDot("wgt-theme-world-beacon", 5, 5, 1016, 262);
        AddAmbientDot("wgt-theme-world-beacon", 6, 6, 1034, 650);
        AddAmbientDot("wgt-theme-world-beacon", 5, 5, 352, 674);
        AddAmbientDot("wgt-theme-world-beacon", 4, 4, 548, 188);
        AddAmbientDot("wgt-theme-world-beacon", 4, 4, 844, 708);

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

    private void AddAmbientField(string className, double width, double height, double left, double top, double radius)
    {
        var field = new Border
        {
            Width = width,
            Height = height,
            CornerRadius = new CornerRadius(radius),
            IsHitTestVisible = false
        };
        field.Classes.Add("wgt-theme-ambient");
        field.Classes.Add(className);
        Canvas.SetLeft(field, left);
        Canvas.SetTop(field, top);
        themeAmbientLayer!.Children.Add(field);
        themeAmbientElements.Add(field);
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
        const double arm = 38;
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
