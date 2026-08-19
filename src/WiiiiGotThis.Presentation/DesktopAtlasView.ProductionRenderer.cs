using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopAtlasView
{
    private AtlasSceneControl? productionSceneRenderer;
    private ShellViewModel? productionRendererShell;

    private bool IsProductionSceneRendererActive => productionSceneRenderer is not null;

    private void EnsureProductionSceneRenderer()
    {
        if (productionSceneRenderer is not null)
            return;

        productionSceneRenderer = new AtlasSceneControl
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            IsHitTestVisible = true
        };
        productionSceneRenderer.NodeInvoked += OnProductionSceneNodeInvoked;
        productionSceneRenderer.NodeActivated += OnProductionSceneNodeActivated;

        // The legacy Canvas stays as migration scaffolding for now, but it is no
        // longer part of the visible or hit-testable Atlas scene.
        SceneCanvas.IsVisible = false;
        SceneCanvas.IsHitTestVisible = false;
        SceneCanvas.Children.Clear();
        if (themeAmbientLayer is not null)
            themeAmbientLayer.IsVisible = false;

        AtlasViewport.Children.Insert(0, productionSceneRenderer);
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
        if (productionSceneRenderer is null || productionRendererShell is null)
            return;

        productionSceneRenderer.SetScene(
            productionRendererShell.AtlasNodes,
            productionRendererShell.AtlasConnections,
            productionRendererShell.SelectedAtlasNode?.NodeId,
            productionRendererShell.AtlasTheme,
            productionRendererShell.IsAtlasReducedMotion);
    }

    private void UpdateProductionSceneCamera()
    {
        productionSceneRenderer?.SetCamera(
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
        AtlasViewport.Focus();
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
