namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasProductionRendererContractTests
{
    [Fact]
    public void Atlas_graph_is_drawn_by_one_custom_scene_control_instead_of_visible_graph_controls()
    {
        var root = FindRepositoryRoot();
        var renderer = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasSceneControl.cs"));
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

        Assert.Contains("public sealed class AtlasSceneControl : Control", renderer, StringComparison.Ordinal);
        Assert.Contains("public override void Render(DrawingContext context)", renderer, StringComparison.Ordinal);
        Assert.Contains("context.DrawGeometry", renderer, StringComparison.Ordinal);
        Assert.Contains("context.DrawEllipse", renderer, StringComparison.Ordinal);
        Assert.Contains("NodeInvoked", renderer, StringComparison.Ordinal);
        Assert.Contains("NodeActivated", renderer, StringComparison.Ordinal);

        Assert.DoesNotContain("new Button", renderer, StringComparison.Ordinal);
        Assert.DoesNotContain("new Border", renderer, StringComparison.Ordinal);
        Assert.DoesNotContain("new Canvas", renderer, StringComparison.Ordinal);
        Assert.Contains("SceneCanvas.IsVisible = false", host, StringComparison.Ordinal);
        Assert.Contains("AtlasViewport.Children.Insert(0, productionSceneRenderer)", host, StringComparison.Ordinal);

        var productionGuard = atlasView.IndexOf("if (IsProductionSceneRendererActive)", StringComparison.Ordinal);
        var legacyMaterialization = atlasView.IndexOf("AddGridLines();", StringComparison.Ordinal);
        Assert.True(productionGuard >= 0, "Production renderer guard must exist before legacy scene materialization.");
        Assert.True(legacyMaterialization > productionGuard, "Legacy control-tree materialization must remain behind the production renderer guard.");
        Assert.Contains("AttachProductionRendererShell(currentShell);", atlasView, StringComparison.Ordinal);
        Assert.Contains("UpdateProductionScene();", atlasView, StringComparison.Ordinal);
    }

    [Fact]
    public void Custom_scene_preserves_focus_theme_motion_and_direct_product_entry_contracts()
    {
        var root = FindRepositoryRoot();
        var renderer = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasSceneControl.cs"));
        var host = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.ProductionRenderer.cs"));

        Assert.Contains("AtlasPresentationFocus.Build", renderer, StringComparison.Ordinal);
        Assert.Contains("AtlasThemePreference.Technical", renderer, StringComparison.Ordinal);
        Assert.Contains("AtlasThemePreference.Elegant", renderer, StringComparison.Ordinal);
        Assert.Contains("AtlasThemePreference.Machine", renderer, StringComparison.Ordinal);
        Assert.Contains("AtlasThemePreference.World", renderer, StringComparison.Ordinal);
        Assert.Contains("RequestAnimationFrame", renderer, StringComparison.Ordinal);
        Assert.Contains("themeTransitionActive = !reducedMotion", renderer, StringComparison.Ordinal);
        Assert.Contains("Focus();", renderer, StringComparison.Ordinal);
        Assert.DoesNotContain("AtlasViewport.Focus();", host, StringComparison.Ordinal);
        Assert.Contains("if (!node.IsEnabled)", host, StringComparison.Ordinal);
        Assert.Contains("await OpenSelectedProductSurfaceAsync()", host, StringComparison.Ordinal);
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
