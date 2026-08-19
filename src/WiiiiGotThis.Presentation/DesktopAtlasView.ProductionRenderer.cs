using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopAtlasView
{
    private AtlasLandscapeControl? productionSceneRenderer;
    private AtlasLivingWorldControl? livingWorldRenderer;
    private AtlasWorldInfrastructureOverlay? livingWorldInfrastructureOverlay;
    private ShellViewModel? productionRendererShell;

    private bool IsProductionSceneRendererActive =>
        productionSceneRenderer is not null
        || livingWorldRenderer is not null
        || livingWorldInfrastructureOverlay is not null;

    private void EnsureProductionSceneRenderer()
    {
        if (productionSceneRenderer is not null
            && livingWorldRenderer is not null
            && livingWorldInfrastructureOverlay is not null)
        {
            return;
        }

        productionSceneRenderer = new AtlasLandscapeControl
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            IsHitTestVisible = true
        };
        productionSceneRenderer.NodeInvoked += OnProductionSceneNodeInvoked;
        productionSceneRenderer.NodeActivated += OnProductionSceneNodeActivated;

        livingWorldRenderer = new AtlasLivingWorldControl
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            IsHitTestVisible = false,
            IsVisible = false
        };
        livingWorldRenderer.NodeInvoked += OnProductionSceneNodeInvoked;
        livingWorldRenderer.NodeActivated += OnProductionSceneNodeActivated;

        livingWorldInfrastructureOverlay = new AtlasWorldInfrastructureOverlay
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            IsHitTestVisible = false,
            IsVisible = false
        };

        // The legacy Canvas and the first custom graph/region experiment stay only as
        // migration evidence. The visible production candidate is either the semantic
        // technical landscape or the dedicated living-world projection over the same
        // Atlas node/connection model.
        SceneCanvas.IsVisible = false;
        SceneCanvas.IsHitTestVisible = false;
        SceneCanvas.Children.Clear();
        ControlHint.IsVisible = false;
        if (themeAmbientLayer is not null)
            themeAmbientLayer.IsVisible = false;

        AtlasViewport.Children.Insert(0, productionSceneRenderer);
        AtlasViewport.Children.Insert(1, livingWorldRenderer);
        AtlasViewport.Children.Insert(2, livingWorldInfrastructureOverlay);
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
        if (productionSceneRenderer is null
            || livingWorldRenderer is null
            || livingWorldInfrastructureOverlay is null
            || productionRendererShell is null)
        {
            return;
        }

        var isLivingWorld = productionRendererShell.AtlasTheme == AtlasThemePreference.World;
        productionSceneRenderer.IsVisible = !isLivingWorld;
        productionSceneRenderer.IsHitTestVisible = !isLivingWorld;
        livingWorldRenderer.IsVisible = isLivingWorld;
        livingWorldRenderer.IsHitTestVisible = isLivingWorld;
        livingWorldInfrastructureOverlay.IsVisible = isLivingWorld;

        productionSceneRenderer.SetScene(
            productionRendererShell.AtlasNodes,
            productionRendererShell.AtlasConnections,
            productionRendererShell.SelectedAtlasNode?.NodeId,
            productionRendererShell.AtlasTheme,
            productionRendererShell.IsAtlasReducedMotion);

        livingWorldRenderer.SetScene(
            productionRendererShell.AtlasNodes,
            productionRendererShell.AtlasConnections,
            productionRendererShell.SelectedAtlasNode?.NodeId,
            productionRendererShell.IsAtlasReducedMotion);

        livingWorldInfrastructureOverlay.SetScene(
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
        livingWorldRenderer?.SetCamera(
            sceneScale.ScaleX,
            sceneTranslate.X,
            sceneTranslate.Y);
        livingWorldInfrastructureOverlay?.SetCamera(
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
            // Provider entry must not terminate the WGT host. Individual provider
            // surfaces own their detailed failure state; this is the last host guard.
        }
    }
}
