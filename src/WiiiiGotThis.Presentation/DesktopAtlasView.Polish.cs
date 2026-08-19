using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using WiiiiGotThis.Application;
using AtlasPath = Avalonia.Controls.Shapes.Path;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopAtlasView
{
    private Canvas? inspectorTetherLayer;
    private AtlasPath? inspectorTether;
    private ShellViewModel? polishShell;
    private bool polishEventsAttached;

    private void OnAtlasPolishAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (polishEventsAttached)
            return;

        polishEventsAttached = true;
        EnsureThemeRenderer();
        EnsureInspectorTether();
        sceneScale.PropertyChanged += OnAtlasCameraTransformChanged;
        sceneTranslate.PropertyChanged += OnAtlasCameraTransformChanged;
        InspectorCard.SizeChanged += OnInspectorSizeChanged;
        AttachPolishShell(DataContext as ShellViewModel);
        AttachExperienceShell(DataContext as ShellViewModel);
        ApplyThemeRenderer(polishShell?.AtlasTheme ?? visualTheme);
        UpdateExperienceState();
        UpdateInspectorTether();
    }

    private void OnAtlasPolishDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (!polishEventsAttached)
            return;

        polishEventsAttached = false;
        sceneScale.PropertyChanged -= OnAtlasCameraTransformChanged;
        sceneTranslate.PropertyChanged -= OnAtlasCameraTransformChanged;
        InspectorCard.SizeChanged -= OnInspectorSizeChanged;
        AttachExperienceShell(null);
        AttachPolishShell(null);
    }

    private void OnAtlasPolishDataContextChanged(object? sender, EventArgs e)
    {
        AttachPolishShell(DataContext as ShellViewModel);
        AttachExperienceShell(DataContext as ShellViewModel);
        ApplyThemeRenderer(polishShell?.AtlasTheme ?? visualTheme);
        UpdateExperienceState();
        UpdateInspectorTether();
    }

    private void AttachPolishShell(ShellViewModel? next)
    {
        if (ReferenceEquals(polishShell, next))
            return;

        if (polishShell is not null)
            polishShell.PropertyChanged -= OnPolishShellPropertyChanged;

        polishShell = next;
        if (polishShell is not null)
            polishShell.PropertyChanged += OnPolishShellPropertyChanged;
    }

    private void OnPolishShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShellViewModel.SelectedAtlasNode))
        {
            UpdateSpatialDepthSelection();
            UpdateInspectorTether();
        }
        else if (e.PropertyName == nameof(ShellViewModel.AtlasTheme) && polishShell is not null)
        {
            ApplyThemeRenderer(polishShell.AtlasTheme);
            ApplyThemeToSpatialDepth();
            UpdateExperienceState();
            if (inspectorTether is not null)
                ApplyThemeClass(inspectorTether);
        }
        else if (e.PropertyName == nameof(ShellViewModel.AtlasSettingsExpanded))
        {
            UpdateExperienceState();
        }
    }

    private void OnAtlasCameraTransformChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => UpdateInspectorTether();

    private void OnInspectorSizeChanged(object? sender, SizeChangedEventArgs e) => UpdateInspectorTether();

    private void EnsureInspectorTether()
    {
        if (inspectorTether is not null)
            return;

        inspectorTether = new AtlasPath
        {
            IsHitTestVisible = false,
            IsVisible = false
        };
        inspectorTether.Classes.Add("wgt-atlas-inspector-tether");
        ApplyThemeClass(inspectorTether);

        inspectorTetherLayer = new Canvas
        {
            IsHitTestVisible = false,
            ClipToBounds = true,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            Children = { inspectorTether }
        };

        // Keep geometry out of the Grid measure pass. A direct Path child can feed its
        // changing geometry back into measure/layout when the inspector appears.
        AtlasViewport.Children.Insert(Math.Min(1, AtlasViewport.Children.Count), inspectorTetherLayer);
    }

    private void UpdateInspectorTether()
    {
        EnsureInspectorTether();
        var tether = inspectorTether!;
        var node = shell?.SelectedAtlasNode;
        if (node is null || !InspectorCard.IsVisible || AtlasViewport.Bounds.Width <= 0)
        {
            tether.IsVisible = false;
            return;
        }

        var world = WorldPoint(node);
        var nodeX = world.X * sceneScale.ScaleX + sceneTranslate.X;
        var nodeY = world.Y * sceneScale.ScaleY + sceneTranslate.Y;
        var left = InspectorCard.Margin.Left;
        var top = InspectorCard.Margin.Top;
        var cardWidth = InspectorCard.Bounds.Width > 0 ? InspectorCard.Bounds.Width : 404d;
        var cardOnRight = left >= nodeX;
        var direction = cardOnRight ? 1d : -1d;
        var nodeRadius = node.Kind switch
        {
            AtlasNodeKind.Core => 92d,
            AtlasNodeKind.Service => 73d,
            _ => 31d
        } * sceneScale.ScaleX;

        var start = new Point(nodeX + direction * nodeRadius, nodeY);
        var end = new Point(
            cardOnRight ? left : left + cardWidth,
            Math.Clamp(nodeY, top + 54d, top + 132d));
        var control = new Point(
            start.X + (end.X - start.X) * 0.58,
            start.Y + (end.Y - start.Y) * 0.18);

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(start, isFilled: false);
            context.QuadraticBezierTo(control, end, isStroked: true);
            context.EndFigure(isClosed: false);
        }

        tether.Data = geometry;
        tether.IsVisible = true;
    }

    private void OnRelationshipSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox { SelectedItem: AtlasRelationshipPresentationViewModel relationship } list || shell is null)
            return;

        list.SelectedItem = null;
        var target = shell.AtlasNodes.FirstOrDefault(node =>
            string.Equals(node.NodeId, relationship.RelatedNodeId, StringComparison.Ordinal));
        if (target is null)
            return;

        shell.SelectAtlasNodeCommand.Execute(target);
        CenterOnSelected();
        AtlasViewport.Focus();
    }
}
