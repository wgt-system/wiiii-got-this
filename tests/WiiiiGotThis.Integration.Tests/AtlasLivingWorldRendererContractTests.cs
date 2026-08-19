namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasLivingWorldRendererContractTests
{
    [Fact]
    public void World_theme_uses_the_authored_v2_renderer_and_keeps_the_first_living_renderer_inactive()
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
            "AtlasWorldV2Control.cs"));
        var legacy = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasLivingWorldControl.cs"));

        Assert.Contains("new AtlasWorldV2Control", host, StringComparison.Ordinal);
        Assert.Contains("AtlasThemePreference.World", host, StringComparison.Ordinal);
        Assert.Contains("worldV2Renderer.IsVisible = isWorld", host, StringComparison.Ordinal);
        Assert.DoesNotContain("new AtlasLivingWorldControl", host, StringComparison.Ordinal);

        Assert.Contains("DrawWgtCity", world, StringComparison.Ordinal);
        Assert.Contains("DrawVocation", world, StringComparison.Ordinal);
        Assert.Contains("DrawIllumination", world, StringComparison.Ordinal);
        Assert.Contains("DrawOrientation", world, StringComparison.Ordinal);
        Assert.Contains("DrawConveyance", world, StringComparison.Ordinal);
        Assert.Contains("DrawRoadNetwork", world, StringComparison.Ordinal);
        Assert.Contains("DrawCapabilityInfrastructure", world, StringComparison.Ordinal);

        // The previous renderer remains readable migration evidence only; retaining the file does
        // not authorize putting it back into the active host to satisfy historical tests.
        Assert.Contains("public sealed class AtlasLivingWorldControl", legacy, StringComparison.Ordinal);
    }

    [Fact]
    public void World_v2_treats_shared_capabilities_as_infrastructure_not_peer_product_towns()
    {
        var root = FindRepositoryRoot();
        var world = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasWorldV2Control.cs"));

        Assert.Contains("DrawConveyanceConsumption", world, StringComparison.Ordinal);
        Assert.Contains("DrawIndustrialGround", world, StringComparison.Ordinal);
        Assert.Contains("DrawWarehouse", world, StringComparison.Ordinal);
        Assert.Contains("DrawRelayMast", world, StringComparison.Ordinal);
        Assert.Contains("BuildAtlasProjectionUseCase.ConveyanceDurableDeliveryCapabilityId", world, StringComparison.Ordinal);
        Assert.Contains("BuildAtlasProjectionUseCase.OrientationGeospatialCapabilityId", world, StringComparison.Ordinal);
        Assert.DoesNotContain("DrawProductSettlements", world, StringComparison.Ordinal);
        Assert.DoesNotContain("CapabilityRevealZoom", world, StringComparison.Ordinal);
    }

    [Fact]
    public void World_v2_has_authored_expansion_sites_for_low_teens_direct_products_before_grouping()
    {
        var root = FindRepositoryRoot();
        var world = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasWorldV2Control.cs"));

        Assert.Contains("ExpansionPlaces", world, StringComparison.Ordinal);
        Assert.Contains("Direct products intentionally remain viable into the low teens", world, StringComparison.Ordinal);
        Assert.Contains("new(-675, -115)", world, StringComparison.Ordinal);
        Assert.Contains("new(775, -65)", world, StringComparison.Ordinal);
        Assert.DoesNotContain("ring < 2", world, StringComparison.Ordinal);
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
