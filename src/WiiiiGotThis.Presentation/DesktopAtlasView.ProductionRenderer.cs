using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopAtlasView
{
    private AtlasLandscapeControl? productionSceneRenderer;
    private AtlasGridControl? atlasGridRenderer;
    private ShellViewModel? productionRendererShell;

    private bool IsProductionSceneRendererActive =>
        productionSceneRenderer is not null || atlasGridRenderer is not null;

    private void EnsureProductionSceneRenderer()
    {
        if (productionSceneRenderer is not null && atlasGridRenderer is not null)
            return;

        productionSceneRenderer = new AtlasLandscapeControl
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            IsHitTestVisible = true
        };
        productionSceneRenderer.NodeInvoked += OnProductionSceneNodeInvoked;
        productionSceneRenderer.NodeActivated += OnProductionSceneNodeActivated;

        atlasGridRenderer = new AtlasGridControl
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            IsHitTestVisible = false,
            IsVisible = false
        };
        atlasGridRenderer.NodeInvoked += OnProductionSceneNodeInvoked;
        atlasGridRenderer.NodeActivated += OnProductionSceneNodeActivated;

        // The legacy Canvas and earlier World/city experiments are not part of the active path.
        // Exactly one renderer is active at a time: the diagnostic landscape for the older
        // appearance themes, or the abstract modular Atlas grid for the flagship World theme.
        SceneCanvas.IsVisible = false;
        SceneCanvas.IsHitTestVisible = false;
        SceneCanvas.Children.Clear();
        ControlHint.IsVisible = false;
        if (themeAmbientLayer is not null)
            themeAmbientLayer.IsVisible = false;

        AtlasViewport.Children.Insert(0, productionSceneRenderer);
        AtlasViewport.Children.Insert(1, atlasGridRenderer);
        UpdateProductionScene();
        UpdateProductionSceneCamera();
    }

    private void AttachProductionRendererShell(ShellViewModel? next)
    {
        if (ReferenceEquals(productionRendererShell, next))
            return;

        if (productionRendererShell is not null)
        {
            productionRendererShell.PropertyChanged -= OnProductionRendererShellPropertyChanged;
            productionRendererShell.AtlasNodes.CollectionChanged -= OnProductionRendererCollectionChanged;
            productionRendererShell.AtlasConnections.CollectionChanged -= OnProductionRendererCollectionChanged;
        }

        productionRendererShell = next;
        if (productionRendererShell is not null)
        {
            productionRendererShell.PropertyChanged += OnProductionRendererShellPropertyChanged;
            productionRendererShell.AtlasNodes.CollectionChanged += OnProductionRendererCollectionChanged;
            productionRendererShell.AtlasConnections.CollectionChanged += OnProductionRendererCollectionChanged;
        }

        UpdateProductionScene();
        UpdateProductionSceneCamera();
    }

    private void OnProductionRendererCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        UpdateProductionScene();

    private void OnProductionRendererShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShellViewModel.SelectedAtlasNode)
            || e.PropertyName == nameof(ShellViewModel.AtlasTheme)
            || e.PropertyName == nameof(ShellViewModel.AtlasMotion)
            || e.PropertyName == nameof(ShellViewModel.IsAtlasReducedMotion))
        {
            UpdateProductionScene();
        }
    }

    private void UpdateProductionScene()
    {
        if (productionSceneRenderer is null || atlasGridRenderer is null || productionRendererShell is null)
            return;

        var isWorld = productionRendererShell.AtlasTheme == AtlasThemePreference.World;
        productionSceneRenderer.IsVisible = !isWorld;
        productionSceneRenderer.IsHitTestVisible = !isWorld;
        atlasGridRenderer.IsVisible = isWorld;
        atlasGridRenderer.IsHitTestVisible = isWorld;

        productionSceneRenderer.SetScene(
            productionRendererShell.AtlasNodes,
            productionRendererShell.AtlasConnections,
            productionRendererShell.SelectedAtlasNode?.NodeId,
            productionRendererShell.AtlasTheme,
            productionRendererShell.IsAtlasReducedMotion);

        atlasGridRenderer.SetScene(
            productionRendererShell.AtlasNodes,
            productionRendererShell.AtlasConnections,
            productionRendererShell.SelectedAtlasNode?.NodeId,
            productionRendererShell.IsAtlasReducedMotion);
    }

    private void UpdateProductionSceneCamera()
    {
        productionSceneRenderer?.SetCamera(
            sceneScale.ScaleX,
            sceneTranslate.X,
            sceneTranslate.Y);
        atlasGridRenderer?.SetCamera(
            sceneScale.ScaleX,
            sceneTranslate.X,
            sceneTranslate.Y);
    }

    private void OnProductionSceneNodeInvoked(AtlasNodePresentationViewModel node)
    {
        if (shell is null)
            return;

        shell.SelectAtlasNodeCommand.Execute(node);
        QueueInspectorPlacementRefinement();
    }

    private async void OnProductionSceneNodeActivated(AtlasNodePresentationViewModel node)
    {
        if (shell is null)
            return;

        shell.SelectAtlasNodeCommand.Execute(node);
        if (!node.IsEnabled)
            return;

        try
        {
            await OpenSelectedProductSurfaceAsync();
        }
        catch (Exception)
        {
            // Provider entry must not terminate the WGT host. Individual provider surfaces own
            // detailed failure state; this remains the last WGT host guard.
        }
    }
}
