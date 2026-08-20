using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using WiiiiGotThis.Application;
using AtlasPath = Avalonia.Controls.Shapes.Path;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopAtlasView
{
    private readonly TranslateTransform inspectorTranslate = new();
    private Canvas? inspectorTetherLayer;
    private AtlasPath? inspectorTether;
    private ShellViewModel? polishShell;
    private bool polishEventsAttached;
    private bool inspectorPlacementQueued;
    private bool inspectorHasPlacement;
    private bool inspectorDragging;
    private Point inspectorDragStart;
    private double inspectorDragOriginX;
    private double inspectorDragOriginY;

    private void OnAtlasPolishAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (polishEventsAttached)
            return;

        polishEventsAttached = true;
        ConfigureSpatialInspectorDossier();
        EnsureThemeRenderer();
        EnsureInspectorTether();
        EnsureProductionSceneRenderer();
        sceneScale.PropertyChanged += OnAtlasCameraTransformChanged;
        sceneTranslate.PropertyChanged += OnAtlasCameraTransformChanged;
        InspectorCard.SizeChanged += OnInspectorSizeChanged;
        AttachPolishShell(DataContext as ShellViewModel);
        AttachExperienceShell(DataContext as ShellViewModel);
        AttachProductionRendererShell(DataContext as ShellViewModel);
        EnsureFinalInspectorSections();
        ApplyThemeRenderer(polishShell?.AtlasTheme ?? visualTheme);
        UpdateExperienceState();
        UpdateFinalInspectorFacts();
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
        AttachProductionRendererShell(null);
        AttachExperienceShell(null);
        AttachPolishShell(null);
    }

    private void OnAtlasPolishDataContextChanged(object? sender, EventArgs e)
    {
        ConfigureSpatialInspectorDossier();
        AttachPolishShell(DataContext as ShellViewModel);
        AttachExperienceShell(DataContext as ShellViewModel);
        AttachProductionRendererShell(DataContext as ShellViewModel);
        EnsureFinalInspectorSections();
        ApplyThemeRenderer(polishShell?.AtlasTheme ?? visualTheme);
        UpdateExperienceState();
        UpdateFinalInspectorFacts();
        QueueInspectorPlacementRefinement();
        UpdateInspectorTether();
    }

    private void ConfigureSpatialInspectorDossier()
    {
        InspectorCard.Width = 300;
        InspectorCard.MaxHeight = 560;
        InspectorCard.Padding = new Thickness(16);
        InspectorCard.RenderTransform = inspectorTranslate;
        EnsureAtlasNavigationChrome();
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
            UpdateFinalInspectorFacts();
            QueueInspectorPlacementRefinement();
            UpdateInspectorTether();
        }
        else if (e.PropertyName == nameof(ShellViewModel.SelectedIntegration)
                 || e.PropertyName == nameof(ShellViewModel.CurrentDeviceName))
        {
            UpdateFinalInspectorFacts();
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
        // Camera movement must never choose a new dossier side/position. The user owns the
        // floating dossier position; only the tether follows the selected Atlas object.
        UpdateProductionSceneCamera();
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
                if (!polishEventsAttached)
                    return;
                RefineInspectorPlacement();
                UpdateInspectorTether();
            },
            DispatcherPriority.Render);
    }

    private void RefineInspectorPlacement()
    {
        if (!InspectorCard.IsVisible || AtlasViewport.Bounds.Width <= 0 || AtlasViewport.Bounds.Height <= 0)
            return;

        var cardWidth = InspectorCard.Bounds.Width > 0 ? InspectorCard.Bounds.Width : 300d;
        var cardHeight = InspectorCard.Bounds.Height > 0
            ? Math.Min(InspectorCard.Bounds.Height, 560d)
            : 500d;
        const double edge = 18d;
        const double topChromeClearance = 92d;

        if (!inspectorHasPlacement)
        {
            inspectorTranslate.X = Math.Max(edge, AtlasViewport.Bounds.Width - cardWidth - 34d);
            inspectorTranslate.Y = Math.Min(
                Math.Max(topChromeClearance, 138d),
                Math.Max(topChromeClearance, AtlasViewport.Bounds.Height - cardHeight - edge));
            inspectorHasPlacement = true;
            return;
        }

        inspectorTranslate.X = Math.Clamp(
            inspectorTranslate.X,
            edge,
            Math.Max(edge, AtlasViewport.Bounds.Width - cardWidth - edge));
        inspectorTranslate.Y = Math.Clamp(
            inspectorTranslate.Y,
            topChromeClearance,
            Math.Max(topChromeClearance, AtlasViewport.Bounds.Height - cardHeight - edge));
    }

    private void OnInspectorHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(InspectorCard);
        var sourceIsButton = e.Source is Button
            || e.Source is Visual visual
            && visual.GetVisualAncestors().OfType<Button>().Any();
        if (!point.Properties.IsLeftButtonPressed || sourceIsButton)
            return;

        inspectorDragging = true;
        inspectorDragStart = e.GetPosition(AtlasViewport);
        inspectorDragOriginX = inspectorTranslate.X;
        inspectorDragOriginY = inspectorTranslate.Y;
        e.Pointer.Capture(InspectorCard);
        e.Handled = true;
    }

    private void OnInspectorDragMoved(object? sender, PointerEventArgs e)
    {
        if (!inspectorDragging)
            return;

        var current = e.GetPosition(AtlasViewport);
        var delta = current - inspectorDragStart;
        inspectorTranslate.X = inspectorDragOriginX + delta.X;
        inspectorTranslate.Y = inspectorDragOriginY + delta.Y;
        RefineInspectorPlacement();
        UpdateInspectorTether();
        e.Handled = true;
    }

    private void OnInspectorDragReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!inspectorDragging)
            return;

        inspectorDragging = false;
        e.Pointer.Capture(null);
        RefineInspectorPlacement();
        UpdateInspectorTether();
        e.Handled = true;
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

        var world = ActiveRendererWorldPoint(node);
        var nodeX = world.X * sceneScale.ScaleX + sceneTranslate.X;
        var nodeY = world.Y * sceneScale.ScaleY + sceneTranslate.Y;
        var left = inspectorTranslate.X;
        var top = inspectorTranslate.Y;
        var cardWidth = InspectorCard.Bounds.Width > 0 ? InspectorCard.Bounds.Width : 300d;
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
            Math.Clamp(nodeY, top + 48d, top + 118d));
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

    private Point ActiveRendererWorldPoint(AtlasNodePresentationViewModel node)
    {
        if (shell?.AtlasTheme == AtlasThemePreference.World
            && atlasGridRenderer is not null
            && atlasGridRenderer.TryGetWorldPosition(node.NodeId, out var authored))
        {
            return new Point(WorldCenterX + authored.X, WorldCenterY + authored.Y);
        }

        return WorldPoint(node);
    }

    private void CenterOnActiveRendererNode(AtlasNodePresentationViewModel node)
    {
        var world = ActiveRendererWorldPoint(node);
        sceneTranslate.X = AtlasViewport.Bounds.Width / 2 - world.X * sceneScale.ScaleX;
        sceneTranslate.Y = AtlasViewport.Bounds.Height / 2 - world.Y * sceneScale.ScaleY;
        PositionInspector();
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
        CenterOnActiveRendererNode(target);
        AtlasViewport.Focus();
    }
}
