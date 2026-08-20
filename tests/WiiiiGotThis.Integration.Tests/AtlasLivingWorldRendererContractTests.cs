namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasLivingWorldRendererContractTests
{
    [Fact]
    public void World_theme_uses_the_abstract_grid_renderer_and_not_any_city_renderer()
    {
        var root = FindRepositoryRoot();
        var host = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.ProductionRenderer.cs"));
        var grid = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasGridControl.cs"));

        Assert.Contains("new AtlasGridControl", host, StringComparison.Ordinal);
        Assert.Contains("AtlasThemePreference.World", host, StringComparison.Ordinal);
        Assert.Contains("atlasGridRenderer.IsVisible = isWorld", host, StringComparison.Ordinal);
        Assert.DoesNotContain("new AtlasWorldV2Control", host, StringComparison.Ordinal);
        Assert.DoesNotContain("new AtlasLivingWorldControl", host, StringComparison.Ordinal);

        Assert.Contains("DrawGrid", grid, StringComparison.Ordinal);
        Assert.Contains("DrawPrimaryNodes", grid, StringComparison.Ordinal);
        Assert.Contains("DrawAmbientField", grid, StringComparison.Ordinal);
        Assert.Contains("DrawInfrastructureBus", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("City", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("Settlement", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("Village", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("Road", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("Terrain", grid, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_capabilities_are_infrastructure_modules_not_peer_product_tiles()
    {
        var root = FindRepositoryRoot();
        var grid = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasGridControl.cs"));

        Assert.Contains("node.IsSharedCapabilityProvider", grid, StringComparison.Ordinal);
        Assert.Contains("DrawInfrastructureNode", grid, StringComparison.Ordinal);
        Assert.Contains("DrawInfrastructureBus", grid, StringComparison.Ordinal);
        Assert.Contains("InfrastructureHeight", grid, StringComparison.Ordinal);
        Assert.Contains("ProductHeight", grid, StringComparison.Ordinal);
        Assert.Contains("DrawVisibleCapabilityPorts", grid, StringComparison.Ordinal);
    }

    [Fact]
    public void Direct_products_scale_through_deterministic_rows_before_semantic_grouping_is_needed()
    {
        var root = FindRepositoryRoot();
        var grid = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasGridControl.cs"));

        Assert.Contains("ProductSlot", grid, StringComparison.Ordinal);
        Assert.Contains("maxColumns = 4", grid, StringComparison.Ordinal);
        Assert.Contains("row = index / maxColumns", grid, StringComparison.Ordinal);
        Assert.Contains("rowCount = Math.Min(maxColumns, count - rowStart)", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("ExpansionPlaces", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("ring < 2", grid, StringComparison.Ordinal);
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
