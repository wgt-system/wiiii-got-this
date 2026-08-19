using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
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
    private bool inspectorPlacementQueued;

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
        QueueInspectorPlacementRefinement();
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
        QueueInspectorPlacementRefinement();
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
            QueueInspectorPlacementRefinement();
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

    private void OnAtlasCameraTransformChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        QueueInspectorPlacementRefinement();
        UpdateInspectorTether();
    }

    private void OnInspectorSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        QueueInspectorPlacementRefinement();
        UpdateInspectorTether();
    }

    private void QueueInspectorPlacementRefinement()
    {
        if (inspectorPlacementQueued)
            return;

        inspectorPlacementQueued = true;
        Dispatcher.UIThread.Post(
            () =>
            {
                inspectorPlacementQueued = false;
                RefineInspectorPlacement();
                UpdateInspectorTether();
            },
            DispatcherPriority.Render);
    }

    private void RefineInspectorPlacement()
    {
        if (shell?.SelectedAtlasNode is not { } node ||
            !InspectorCard.IsVisible ||
            AtlasViewport.Bounds.Width <= 0 ||
            AtlasViewport.Bounds.Height <= 0)
        {
            return;
        }

        var world = WorldPoint(node);
        var nodeX = world.X * sceneScale.ScaleX + sceneTranslate.X;
        var nodeY = world.Y * sceneScale.ScaleY + sceneTranslate.Y;
        var cardWidth = InspectorCard.Bounds.Width > 0 ? InspectorCard.Bounds.Width : 372d;
        var cardHeight = InspectorCard.Bounds.Height > 0
            ? Math.Min(InspectorCard.Bounds.Height, 600d)
            : 540d;
        const double gap = 28d;
        const double edge = 18d;
        const double topChromeClearance = 78d;

        var left = nodeX + gap;
        if (left + cardWidth > AtlasViewport.Bounds.Width - edge)
            left = nodeX - cardWidth - gap;
        left = Math.Clamp(left, edge, Math.Max(edge, AtlasViewport.Bounds.Width - cardWidth - edge));

        var top = Math.Clamp(
            nodeY - 110d,
            topChromeClearance,
            Math.Max(topChromeClearance, AtlasViewport.Bounds.Height - cardHeight - edge));

        var next = new Thickness(left, top, 0, 0);
        if (Math.Abs(InspectorCard.Margin.Left - next.Left) > 0.5 ||
            Math.Abs(InspectorCard.Margin.Top - next.Top) > 0.5)
        {
            InspectorCard.Margin = next;
        }
    }

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
        var cardWidth = InspectorCard.Bounds.Width > 0 ? InspectorCard.Bounds.Width : 372d;
        var cardOnRight = left >= nodeX;
        var direction = cardOnRight ? 1d : -1d;
        var nodeRadius = node.Kind switch
        {
            AtlasNodeKind.Core => 94d,
            AtlasNodeKind.Service => 74d,
            _ => 80d
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
