namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasVisualCompositionTests
{
    [Fact]
    public void Final_renderer_replaces_debug_chrome_with_product_node_emblems()
    {
        var root = FindRepositoryRoot();
        var renderer = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.FinalRenderer.cs"));
        var experience = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.Experience.cs"));

        Assert.Contains("ControlHint.IsVisible = false", renderer, StringComparison.Ordinal);
        Assert.Contains("ThemeMenuButton.IsVisible = false", renderer, StringComparison.Ordinal);
        Assert.Contains("wgt-node-emblem", renderer, StringComparison.Ordinal);
        Assert.Contains("BuildCapabilityPort", renderer, StringComparison.Ordinal);
        Assert.Contains("UpgradeFinalNodeVisuals();", experience, StringComparison.Ordinal);
        Assert.DoesNotContain("outerDiameter", experience, StringComparison.Ordinal);
        Assert.DoesNotContain("innerDiameter", experience, StringComparison.Ordinal);
    }

    [Fact]
    public void Final_styles_keep_the_four_renderer_canvases_visibly_distinct()
    {
        var root = FindRepositoryRoot();
        var styles = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "Styles",
            "AtlasFinalStyles.axaml"));
        var app = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "App.axaml"));

        Assert.Contains("Grid.wgt-atlas-root.theme-technical", styles, StringComparison.Ordinal);
        Assert.Contains("#FF060D13", styles, StringComparison.Ordinal);
        Assert.Contains("Grid.wgt-atlas-root.theme-elegant", styles, StringComparison.Ordinal);
        Assert.Contains("#FF121014", styles, StringComparison.Ordinal);
        Assert.Contains("Grid.wgt-atlas-root.theme-machine", styles, StringComparison.Ordinal);
        Assert.Contains("#FF030A08", styles, StringComparison.Ordinal);
        Assert.Contains("Grid.wgt-atlas-root.theme-world", styles, StringComparison.Ordinal);
        Assert.Contains("#FF07110D", styles, StringComparison.Ordinal);
        Assert.Contains("AtlasFinalStyles.axaml", app, StringComparison.Ordinal);
    }

    [Fact]
    public void World_renderer_uses_sparse_fields_and_beacons_not_orbital_ellipses()
    {
        var root = FindRepositoryRoot();
        var renderer = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.ThemeRenderer.cs"));

        Assert.Contains("wgt-theme-world-field", renderer, StringComparison.Ordinal);
        Assert.Contains("wgt-theme-world-beacon", renderer, StringComparison.Ordinal);
        Assert.DoesNotContain("wgt-theme-world-orbit", renderer, StringComparison.Ordinal);
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
