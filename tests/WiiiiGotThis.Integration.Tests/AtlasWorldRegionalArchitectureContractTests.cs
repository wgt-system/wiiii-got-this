namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasWorldRegionalArchitectureContractTests
{
    [Fact]
    public void World_v2_gives_each_first_class_product_a_materially_different_built_form()
    {
        var root = FindRepositoryRoot();
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

        // Vocation: civic/work town.
        Assert.Contains("DrawPitchedBuilding", world, StringComparison.Ordinal);
        Assert.Contains("DrawCivicBuilding", world, StringComparison.Ordinal);
        Assert.Contains("DrawLongBuilding", world, StringComparison.Ordinal);
        Assert.Contains("DrawWaterTower", world, StringComparison.Ordinal);
        Assert.Contains("DrawMarketSquare", world, StringComparison.Ordinal);

        // Illumination: campus/knowledge settlement.
        Assert.Contains("DrawCampusCourt", world, StringComparison.Ordinal);
        Assert.Contains("DrawPavilion", world, StringComparison.Ordinal);
        Assert.Contains("DrawArcade", world, StringComparison.Ordinal);

        // Orientation: survey/navigation settlement.
        Assert.Contains("DrawSurveyTerrace", world, StringComparison.Ordinal);
        Assert.Contains("DrawObservationTower", world, StringComparison.Ordinal);
        Assert.Contains("DrawSurveyMast", world, StringComparison.Ordinal);
        Assert.Contains("DrawBearingRose", world, StringComparison.Ordinal);

        // WGT and Conveyance retain materially different urban/industrial scales.
        Assert.Contains("DrawCityBlock", world, StringComparison.Ordinal);
        Assert.Contains("DrawCentralTower", world, StringComparison.Ordinal);
        Assert.Contains("DrawWarehouse", world, StringComparison.Ordinal);
        Assert.Contains("DrawSilo", world, StringComparison.Ordinal);
        Assert.DoesNotContain("new AtlasWorldRegionalArchitectureOverlay", host, StringComparison.Ordinal);
    }

    [Fact]
    public void Authored_world_places_stay_presentation_only_and_interact_through_projected_nodes()
    {
        var root = FindRepositoryRoot();
        var world = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasWorldV2Control.cs"));

        Assert.Contains("HitTestPlace", world, StringComparison.Ordinal);
        Assert.Contains("NodeInvoked?.Invoke(node)", world, StringComparison.Ordinal);
        Assert.Contains("NodeActivated?.Invoke(node)", world, StringComparison.Ordinal);
        Assert.Contains("nodes.Where(node => node.IsCore || node.IsService)", world, StringComparison.Ordinal);
        Assert.DoesNotContain("new ServiceIdentity", world, StringComparison.Ordinal);
        Assert.DoesNotContain("new CapabilityIdentity", world, StringComparison.Ordinal);
    }

    [Fact]
    public void Authored_known_product_places_are_asymmetric_and_shared_with_host_alignment()
    {
        var root = FindRepositoryRoot();
        var world = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasWorldV2Control.cs"));
        var polish = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.Polish.cs"));

        Assert.Contains("[\"vocation\"] = new(-500, 150)", world, StringComparison.Ordinal);
        Assert.Contains("[\"illumination\"] = new(-270, -330)", world, StringComparison.Ordinal);
        Assert.Contains("[\"orientation\"] = new(455, -175)", world, StringComparison.Ordinal);
        Assert.Contains("[\"conveyance\"] = new(500, 315)", world, StringComparison.Ordinal);
        Assert.Contains("TryGetWorldPosition", world, StringComparison.Ordinal);
        Assert.Contains("worldV2Renderer.TryGetWorldPosition", polish, StringComparison.Ordinal);
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
