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
    private AtlasWorldEnvironmentalDetailOverlay? livingWorldEnvironmentalOverlay;
    private AtlasWorldRegionalArchitectureOverlay? livingWorldRegionalArchitectureOverlay;
    private AtlasWorldInfrastructureOverlay? livingWorldInfrastructureOverlay;
    private ShellViewModel? productionRendererShell;

    private bool IsProductionSceneRendererActive =>
        productionSceneRenderer is not null
        || livingWorldRenderer is not null
        || livingWorldEnvironmentalOverlay is not null
        || livingWorldRegionalArchitectureOverlay is not null
        || livingWorldInfrastructureOverlay is not null;

    private void EnsureProductionSceneRenderer()
    {
        if (productionSceneRenderer is not null
            && livingWorldRenderer is not null
            && livingWorldEnvironmentalOverlay is not null
            && livingWorldRegionalArchitectureOverlay is not null
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

        livingWorldEnvironmentalOverlay = new AtlasWorldEnvironmentalDetailOverlay
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            IsHitTestVisible = false,
            IsVisible = false
        };

        livingWorldRegionalArchitectureOverlay = new AtlasWorldRegionalArchitectureOverlay
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            IsHitTestVisible = false,
            IsVisible = false
        };

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
        AtlasViewport.Children.Insert(2, livingWorldEnvironmentalOverlay);
        AtlasViewport.Children.Insert(3, livingWorldRegionalArchitectureOverlay);
        AtlasViewport.Children.Insert(4, livingWorldInfrastructureOverlay);
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
            || livingWorldEnvironmentalOverlay is null
            || livingWorldRegionalArchitectureOverlay is null
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
        livingWorldEnvironmentalOverlay.IsVisible = isLivingWorld;
        livingWorldRegionalArchitectureOverlay.IsVisible = isLivingWorld;
        livingWorldInfrastructureOverlay.IsVisible = isLivingWorld;

        productionSceneRenderer.SetScene(
            productionRendererShell.AtlasNodes,
            productionRendererShell.AtlasConnections,
            productionRendererShell.SelectedAtlasNode?.NodeId,
            productionRendererShell.AtlasTheme,
            productionRendererShell.IsAtlasReducedMotion);

        // World presents shared capability consumption through the dedicated infrastructure
        // overlay. Do not also feed the same shared capability node/edge into the base town
        // renderer, otherwise a local relay/factory gets duplicated by a generic graph-like
        // route/building near the shared provider yard.
        var worldBaseNodes = productionRendererShell.AtlasNodes
            .Where(node => !(node.IsCapability && node.ProductRole == AtlasProductRole.SharedCapabilityProvider))
            .ToArray();
        var worldBaseNodeIds = worldBaseNodes
            .Select(node => node.NodeId)
            .ToHashSet(StringComparer.Ordinal);
        var worldBaseConnections = productionRendererShell.AtlasConnections
            .Where(connection =>
                worldBaseNodeIds.Contains(connection.Source.NodeId)
                && worldBaseNodeIds.Contains(connection.Target.NodeId))
            .ToArray();

        livingWorldRenderer.SetScene(
            worldBaseNodes,
            worldBaseConnections,
            productionRendererShell.SelectedAtlasNode?.NodeId,
            productionRendererShell.IsAtlasReducedMotion);

        // Regional architecture is presentation-only but consumes the same projected service
        // identities/selection so its landmarks follow availability/focus without owning any
        // provider capability or interaction semantics.
        livingWorldRegionalArchitectureOverlay.SetScene(
            worldBaseNodes,
            productionRendererShell.SelectedAtlasNode?.NodeId);

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
        livingWorldEnvironmentalOverlay?.SetCamera(
            sceneScale.ScaleX,
            sceneTranslate.X,
            sceneTranslate.Y);
        livingWorldRegionalArchitectureOverlay?.SetCamera(
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
