namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasWorldRegionalArchitectureContractTests
{
    [Fact]
    public void Flagship_grid_differentiates_core_products_and_shared_infrastructure_without_pictorial_metaphors()
    {
        var root = FindRepositoryRoot();
        var grid = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasGridControl.cs"));
        var host = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.ProductionRenderer.cs"));

        Assert.Contains("CoreWidth", grid, StringComparison.Ordinal);
        Assert.Contains("ProductWidth", grid, StringComparison.Ordinal);
        Assert.Contains("InfrastructureWidth", grid, StringComparison.Ordinal);
        Assert.Contains("CapabilityWidth", grid, StringComparison.Ordinal);
        Assert.Contains("DrawProductNode", grid, StringComparison.Ordinal);
        Assert.Contains("DrawInfrastructureNode", grid, StringComparison.Ordinal);
        Assert.Contains("DrawNodeGlyph", grid, StringComparison.Ordinal);
        Assert.Contains("DrawStatusDot", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("Building", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("Tower", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("Warehouse", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("Pavilion", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("Market", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("new AtlasWorldRegionalArchitectureOverlay", host, StringComparison.Ordinal);
    }

    [Fact]
    public void Grid_positions_stay_presentation_only_and_interact_through_projected_nodes()
    {
        var root = FindRepositoryRoot();
        var grid = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasGridControl.cs"));

        Assert.Contains("HitTestNode", grid, StringComparison.Ordinal);
        Assert.Contains("NodeInvoked?.Invoke(node)", grid, StringComparison.Ordinal);
        Assert.Contains("NodeActivated?.Invoke(node)", grid, StringComparison.Ordinal);
        Assert.Contains("nodes.Where(node => node.IsCore || node.IsService)", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("new ServiceIdentity", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("new CapabilityIdentity", grid, StringComparison.Ordinal);
    }

    [Fact]
    public void Known_products_use_deterministic_grid_slots_shared_with_host_alignment()
    {
        var root = FindRepositoryRoot();
        var grid = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasGridControl.cs"));
        var polish = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.Polish.cs"));

        Assert.Contains("PreferredProductOrder", grid, StringComparison.Ordinal);
        Assert.Contains("ProductSlot", grid, StringComparison.Ordinal);
        Assert.Contains("columnGap = 292d", grid, StringComparison.Ordinal);
        Assert.Contains("rowGap = 188d", grid, StringComparison.Ordinal);
        Assert.Contains("TryGetWorldPosition", grid, StringComparison.Ordinal);
        Assert.Contains("atlasGridRenderer.TryGetWorldPosition", polish, StringComparison.Ordinal);
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
