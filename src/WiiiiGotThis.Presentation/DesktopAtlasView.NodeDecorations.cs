using Avalonia;
using Avalonia.Controls;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopAtlasView
{
    private Canvas? themeNodeDecorationLayer;

    private void RebuildThemeNodeDecorations()
    {
        if (themeNodeDecorationLayer is not null)
            SceneCanvas.Children.Remove(themeNodeDecorationLayer);

        var currentShell = experienceShell ?? shell;
        if (currentShell is null)
        {
            themeNodeDecorationLayer = null;
            return;
        }

        themeNodeDecorationLayer = new Canvas
        {
            Width = WorldWidth,
            Height = WorldHeight,
            IsHitTestVisible = false
        };
        themeNodeDecorationLayer.Classes.Add("wgt-atlas-node-decoration-layer");

        foreach (var node in currentShell.AtlasNodes.Where(node => node.IsCore || node.IsService))
        {
            AddTechnicalTicks(node);
            AddElegantHalo(node);
            AddMachineSockets(node);
            AddWorldLandmark(node);
        }

        var insertIndex = 0;
        while (insertIndex < SceneCanvas.Children.Count && SceneCanvas.Children[insertIndex] is Avalonia.Controls.Shapes.Line)
            insertIndex++;
        SceneCanvas.Children.Insert(insertIndex, themeNodeDecorationLayer);
        UpdateThemeNodeDecorationSelection();
    }

    private void AddTechnicalTicks(AtlasNodePresentationViewModel node)
    {
        var point = DecorationCenter(node);
        var radius = node.IsCore ? 78d : 58d;
        AddDecorationRail(node, "technical-tick", 18, 1, point.X - 9, point.Y - radius);
        AddDecorationRail(node, "technical-tick", 18, 1, point.X - 9, point.Y + radius);
        AddDecorationRail(node, "technical-tick", 1, 18, point.X - radius, point.Y - 9);
        AddDecorationRail(node, "technical-tick", 1, 18, point.X + radius, point.Y - 9);
    }

    private void AddElegantHalo(AtlasNodePresentationViewModel node)
    {
        var point = DecorationCenter(node);
        var diameter = node.IsCore ? 162d : 112d;
        var halo = new Border
        {
            Width = diameter,
            Height = diameter,
            CornerRadius = new CornerRadius(diameter / 2),
            DataContext = node,
            IsHitTestVisible = false
        };
        halo.Classes.Add("wgt-node-decoration");
        halo.Classes.Add("elegant-halo");
        ApplyThemeClass(halo);
        Canvas.SetLeft(halo, point.X - diameter / 2);
        Canvas.SetTop(halo, point.Y - diameter / 2);
        themeNodeDecorationLayer!.Children.Add(halo);
    }

    private void AddMachineSockets(AtlasNodePresentationViewModel node)
    {
        var point = DecorationCenter(node);
        var half = node.IsCore ? 78d : 58d;
        const double arm = 18d;
        const double thickness = 2d;

        AddDecorationRail(node, "machine-socket", arm, thickness, point.X - half, point.Y - half);
        AddDecorationRail(node, "machine-socket", thickness, arm, point.X - half, point.Y - half);
        AddDecorationRail(node, "machine-socket", arm, thickness, point.X + half - arm, point.Y - half);
        AddDecorationRail(node, "machine-socket", thickness, arm, point.X + half - thickness, point.Y - half);
        AddDecorationRail(node, "machine-socket", arm, thickness, point.X - half, point.Y + half - thickness);
        AddDecorationRail(node, "machine-socket", thickness, arm, point.X - half, point.Y + half - arm);
        AddDecorationRail(node, "machine-socket", arm, thickness, point.X + half - arm, point.Y + half - thickness);
        AddDecorationRail(node, "machine-socket", thickness, arm, point.X + half - thickness, point.Y + half - arm);
    }

    private void AddWorldLandmark(AtlasNodePresentationViewModel node)
    {
        var point = DecorationCenter(node);
        var width = node.IsCore ? 172d : 128d;
        var height = node.IsCore ? 58d : 44d;
        var baseField = new Border
        {
            Width = width,
            Height = height,
            CornerRadius = new CornerRadius(height / 2),
            DataContext = node,
            BorderThickness = new Thickness(1),
            IsHitTestVisible = false
        };
        baseField.Classes.Add("wgt-node-decoration");
        baseField.Classes.Add("world-landmark");
        ApplyThemeClass(baseField);
        Canvas.SetLeft(baseField, point.X - width / 2);
        Canvas.SetTop(baseField, point.Y + (node.IsCore ? 32d : 27d));
        themeNodeDecorationLayer!.Children.Add(baseField);

        if (node.IsService)
        {
            var beacon = new Border
            {
                Width = 5,
                Height = 5,
                CornerRadius = new CornerRadius(3),
                DataContext = node,
                IsHitTestVisible = false
            };
            beacon.Classes.Add("wgt-node-decoration");
            beacon.Classes.Add("world-node-beacon");
            ApplyThemeClass(beacon);
            Canvas.SetLeft(beacon, point.X + 48);
            Canvas.SetTop(beacon, point.Y + 18);
            themeNodeDecorationLayer!.Children.Add(beacon);
        }
    }

    private void AddDecorationRail(
        AtlasNodePresentationViewModel node,
        string decorationClass,
        double width,
        double height,
        double left,
        double top)
    {
        var rail = new Border
        {
            Width = width,
            Height = height,
            DataContext = node,
            IsHitTestVisible = false
        };
        rail.Classes.Add("wgt-node-decoration");
        rail.Classes.Add(decorationClass);
        ApplyThemeClass(rail);
        Canvas.SetLeft(rail, left);
        Canvas.SetTop(rail, top);
        themeNodeDecorationLayer!.Children.Add(rail);
    }

    private static Point DecorationCenter(AtlasNodePresentationViewModel node)
    {
        var point = WorldPoint(node);
        return new Point(point.X, point.Y - (node.IsCore ? 18d : 24d));
    }

    private void UpdateThemeNodeDecorationSelection()
    {
        if (themeNodeDecorationLayer is null)
            return;

        var selectedId = shell?.SelectedAtlasNode?.NodeId;
        var focusNodeIds = BuildFocusNodeSet(selectedId);
        foreach (var element in themeNodeDecorationLayer.Children.OfType<StyledElement>())
        {
            if (element.DataContext is not AtlasNodePresentationViewModel node)
                continue;

            var selected = string.Equals(node.NodeId, selectedId, StringComparison.Ordinal);
            var contextual = selectedId is not null && !selected && focusNodeIds.Contains(node.NodeId);
            var dimmed = selectedId is not null && !focusNodeIds.Contains(node.NodeId);
            SetStateClass(element, "selected", selected);
            SetStateClass(element, "contextual", contextual);
            SetStateClass(element, "dimmed", dimmed);
        }
    }

    private void ApplyThemeToNodeDecorations()
    {
        if (themeNodeDecorationLayer is null)
            return;

        foreach (var element in themeNodeDecorationLayer.Children.OfType<StyledElement>())
            ApplyThemeClass(element);
        UpdateThemeNodeDecorationSelection();
    }
}
