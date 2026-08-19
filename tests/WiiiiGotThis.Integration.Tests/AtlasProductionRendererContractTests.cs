namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasProductionRendererContractTests
{
    [Fact]
    public void Atlas_uses_one_active_custom_renderer_per_visual_language()
    {
        var root = FindRepositoryRoot();
        var renderer = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasLandscapeControl.cs"));
        var topology = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasLandscape.cs"));
        var world = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasWorldV2Control.cs"));
        var host = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.ProductionRenderer.cs"));
        var atlasView = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.axaml.cs"));

        Assert.Contains("public sealed class AtlasLandscapeControl : Control", renderer, StringComparison.Ordinal);
        Assert.Contains("AtlasLandscapeBuilder.Build", renderer, StringComparison.Ordinal);
        Assert.Contains("public override void Render(DrawingContext context)", renderer, StringComparison.Ordinal);
        Assert.Contains("DrawRegions", renderer, StringComparison.Ordinal);
        Assert.Contains("DrawRoutes", renderer, StringComparison.Ordinal);
        Assert.Contains("DrawCoreNexus", renderer, StringComparison.Ordinal);

        Assert.Contains("public sealed class AtlasWorldV2Control : Control", world, StringComparison.Ordinal);
        Assert.Contains("private AtlasWorldV2Control? worldV2Renderer", host, StringComparison.Ordinal);
        Assert.Contains("worldV2Renderer = new AtlasWorldV2Control", host, StringComparison.Ordinal);
        Assert.Contains("worldV2Renderer.IsVisible = isWorld", host, StringComparison.Ordinal);
        Assert.Contains("productionSceneRenderer.IsVisible = !isWorld", host, StringComparison.Ordinal);
        Assert.DoesNotContain("new AtlasLivingWorldControl", host, StringComparison.Ordinal);
        Assert.DoesNotContain("new AtlasWorldEnvironmentalDetailOverlay", host, StringComparison.Ordinal);
        Assert.DoesNotContain("new AtlasWorldRegionalArchitectureOverlay", host, StringComparison.Ordinal);
        Assert.DoesNotContain("new AtlasWorldInfrastructureOverlay", host, StringComparison.Ordinal);

        Assert.Contains("public sealed record AtlasLandscape", topology, StringComparison.Ordinal);
        Assert.Contains("AtlasLandscapeRouteKind.CrossServiceDependency", topology, StringComparison.Ordinal);

        Assert.DoesNotContain("new Button", renderer, StringComparison.Ordinal);
        Assert.DoesNotContain("new Border", renderer, StringComparison.Ordinal);
        Assert.DoesNotContain("new Canvas", renderer, StringComparison.Ordinal);
        Assert.Contains("SceneCanvas.IsVisible = false", host, StringComparison.Ordinal);
        Assert.Contains("ControlHint.IsVisible = false", host, StringComparison.Ordinal);
        Assert.Contains("AtlasViewport.Children.Insert(0, productionSceneRenderer)", host, StringComparison.Ordinal);
        Assert.Contains("AtlasViewport.Children.Insert(1, worldV2Renderer)", host, StringComparison.Ordinal);

        var productionGuard = atlasView.IndexOf("if (IsProductionSceneRendererActive)", StringComparison.Ordinal);
        var legacyMaterialization = atlasView.IndexOf("AddGridLines();", StringComparison.Ordinal);
        Assert.True(productionGuard >= 0, "Production renderer guard must exist before legacy scene materialization.");
        Assert.True(legacyMaterialization > productionGuard, "Legacy control-tree materialization must remain behind the production renderer guard.");
        Assert.Contains("AttachProductionRendererShell(currentShell);", atlasView, StringComparison.Ordinal);
        Assert.Contains("UpdateProductionScene();", atlasView, StringComparison.Ordinal);
    }

    [Fact]
    public void Active_renderers_preserve_focus_motion_camera_and_direct_product_entry_contracts()
    {
        var root = FindRepositoryRoot();
        var renderer = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasLandscapeControl.cs"));
        var world = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasWorldV2Control.cs"));
        var host = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.ProductionRenderer.cs"));

        Assert.Contains("AtlasPresentationFocus.Build", renderer, StringComparison.Ordinal);
        Assert.Contains("AtlasThemePreference.Technical", renderer, StringComparison.Ordinal);
        Assert.Contains("RequestAnimationFrame", renderer, StringComparison.Ordinal);
        Assert.Contains("themeTransitionActive = !reducedMotion", renderer, StringComparison.Ordinal);
        Assert.Contains("Focus();", renderer, StringComparison.Ordinal);

        Assert.Contains("AtlasPresentationFocus.Build", world, StringComparison.Ordinal);
        Assert.Contains("RequestAnimationFrame", world, StringComparison.Ordinal);
        Assert.Contains("reducedMotion", world, StringComparison.Ordinal);
        Assert.Contains("NodeInvoked", world, StringComparison.Ordinal);
        Assert.Contains("NodeActivated", world, StringComparison.Ordinal);
        Assert.Contains("Focus();", world, StringComparison.Ordinal);

        Assert.DoesNotContain("AtlasViewport.Focus();", host, StringComparison.Ordinal);
        Assert.Contains("if (!node.IsEnabled)", host, StringComparison.Ordinal);
        Assert.Contains("await OpenSelectedProductSurfaceAsync()", host, StringComparison.Ordinal);
        Assert.Contains("worldV2Renderer?.SetCamera", host, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "WiiiiGotThis.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Wiiii Got This repository root from the test output directory.");
    }
}
