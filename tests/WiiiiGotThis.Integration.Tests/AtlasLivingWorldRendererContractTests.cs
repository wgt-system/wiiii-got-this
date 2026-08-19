namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasLivingWorldRendererContractTests
{
    [Fact]
    public void World_theme_uses_a_dedicated_living_renderer_with_real_settlement_primitives()
    {
        var root = FindRepositoryRoot();
        var host = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.ProductionRenderer.cs"));
        var world = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasLivingWorldControl.cs"));

        Assert.Contains("new AtlasLivingWorldControl", host, StringComparison.Ordinal);
        Assert.Contains("AtlasThemePreference.World", host, StringComparison.Ordinal);
        Assert.Contains("livingWorldRenderer.IsVisible = isLivingWorld", host, StringComparison.Ordinal);

        Assert.Contains("DrawWgtCity", world, StringComparison.Ordinal);
        Assert.Contains("DrawProductSettlements", world, StringComparison.Ordinal);
        Assert.Contains("DrawBuilding", world, StringComparison.Ordinal);
        Assert.Contains("DrawWarehouse", world, StringComparison.Ordinal);
        Assert.Contains("DrawTree", world, StringComparison.Ordinal);
        Assert.Contains("DrawRoadNetwork", world, StringComparison.Ordinal);
        Assert.Contains("DrawCapabilityPlaces", world, StringComparison.Ordinal);
    }

    [Fact]
    public void World_theme_treats_shared_capability_providers_as_infrastructure_and_reveals_capabilities_locally()
    {
        var root = FindRepositoryRoot();
        var world = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasLivingWorldControl.cs"));

        Assert.Contains("DrawConveyanceFacility", world, StringComparison.Ordinal);
        Assert.Contains("RELAY YARD", world, StringComparison.Ordinal);
        Assert.Contains("node.IsPrimaryProductProvider", world, StringComparison.Ordinal);
        Assert.Contains("node.IsSharedCapabilityProvider", world, StringComparison.Ordinal);
        Assert.Contains("CapabilityRevealZoom", world, StringComparison.Ordinal);
        Assert.Contains("DrawCapabilityBuilding", world, StringComparison.Ordinal);
    }

    [Fact]
    public void World_layout_supports_direct_products_beyond_the_original_four_before_grouping()
    {
        var root = FindRepositoryRoot();
        var world = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasLivingWorldControl.cs"));
        var presentation = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasPresentation.cs"));

        Assert.Contains("ring < 2", world, StringComparison.Ordinal);
        Assert.Contains("500d : 650d", world, StringComparison.Ordinal);
        Assert.Contains("<= 15 => 470d", presentation, StringComparison.Ordinal);
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
