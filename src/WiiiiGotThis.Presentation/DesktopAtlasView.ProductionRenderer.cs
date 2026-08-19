using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopAtlasView
{
    private AtlasLandscapeControl? productionSceneRenderer;
    private AtlasWorldV2Control? worldV2Renderer;
    private ShellViewModel? productionRendererShell;

    private bool IsProductionSceneRendererActive =>
        productionSceneRenderer is not null || worldV2Renderer is not null;

    private void EnsureProductionSceneRenderer()
    {
        if (productionSceneRenderer is not null && worldV2Renderer is not null)
            return;

        productionSceneRenderer = new AtlasLandscapeControl
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            IsHitTestVisible = true
        };
        productionSceneRenderer.NodeInvoked += OnProductionSceneNodeInvoked;
        productionSceneRenderer.NodeActivated += OnProductionSceneNodeActivated;

        worldV2Renderer = new AtlasWorldV2Control
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            IsHitTestVisible = false,
            IsVisible = false
        };
        worldV2Renderer.NodeInvoked += OnProductionSceneNodeInvoked;
        worldV2Renderer.NodeActivated += OnProductionSceneNodeActivated;

        // The legacy Canvas and the previous World experiments remain in source only as
        // migration evidence. The active product path is now exactly one renderer at a time:
        // semantic diagnostic landscape for non-World themes, authored World V2 for World.
        SceneCanvas.IsVisible = false;
        SceneCanvas.IsHitTestVisible = false;
        SceneCanvas.Children.Clear();
        ControlHint.IsVisible = false;
        if (themeAmbientLayer is not null)
            themeAmbientLayer.IsVisible = false;

        AtlasViewport.Children.Insert(0, productionSceneRenderer);
        AtlasViewport.Children.Insert(1, worldV2Renderer);
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
        if (productionSceneRenderer is null || worldV2Renderer is null || productionRendererShell is null)
            return;

        var isWorld = productionRendererShell.AtlasTheme == AtlasThemePreference.World;
        productionSceneRenderer.IsVisible = !isWorld;
        productionSceneRenderer.IsHitTestVisible = !isWorld;
        worldV2Renderer.IsVisible = isWorld;
        worldV2Renderer.IsHitTestVisible = isWorld;

        productionSceneRenderer.SetScene(
            productionRendererShell.AtlasNodes,
            productionRendererShell.AtlasConnections,
            productionRendererShell.SelectedAtlasNode?.NodeId,
            productionRendererShell.AtlasTheme,
            productionRendererShell.IsAtlasReducedMotion);

        // World receives the complete curated Atlas projection. Shared providers/capabilities are
        // intentionally retained because World V2 turns them into local facilities and networks
        // rather than duplicating them as generic product settlements or graph edges.
        worldV2Renderer.SetScene(
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
        worldV2Renderer?.SetCamera(
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
