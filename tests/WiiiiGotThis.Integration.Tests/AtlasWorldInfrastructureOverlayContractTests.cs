namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasWorldInfrastructureOverlayContractTests
{
    [Fact]
    public void World_projects_shared_capability_consumption_as_a_local_facility_and_backbone()
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

        Assert.Contains("DrawConveyanceConsumption", world, StringComparison.Ordinal);
        Assert.Contains("connection.IsEnabled", world, StringComparison.Ordinal);
        Assert.Contains("connection.IsCapabilityUse", world, StringComparison.Ordinal);
        Assert.Contains("BuildAtlasProjectionUseCase.ConveyanceDurableDeliveryCapabilityId", world, StringComparison.Ordinal);
        Assert.Contains("var facility = vocationCenter + new Vector(", world, StringComparison.Ordinal);
        Assert.Contains("DrawIndustrialGround(context, facility", world, StringComparison.Ordinal);
        Assert.Contains("DrawWarehouse(context, facility", world, StringComparison.Ordinal);
        Assert.Contains("DrawRelayMast(context, facility", world, StringComparison.Ordinal);
        Assert.Contains("OpenWorldRoute(route)", world, StringComparison.Ordinal);
        Assert.Contains("worldV2Renderer = new AtlasWorldV2Control", host, StringComparison.Ordinal);
        Assert.DoesNotContain("new AtlasWorldInfrastructureOverlay", host, StringComparison.Ordinal);
    }

    [Fact]
    public void Local_facility_is_a_projection_of_one_enabled_consumption_not_an_extra_provider()
    {
        var root = FindRepositoryRoot();
        var world = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasWorldV2Control.cs"));

        Assert.Contains("string.Equals(connection.Source.ServiceIdentity?.Value, \"vocation\"", world, StringComparison.Ordinal);
        Assert.Contains("string.Equals(connection.Target.CapabilityIdentity?.Value, BuildAtlasProjectionUseCase.ConveyanceDurableDeliveryCapabilityId", world, StringComparison.Ordinal);
        Assert.Contains("TryService(\"conveyance\"", world, StringComparison.Ordinal);
        Assert.DoesNotContain("new ServiceIdentity", world, StringComparison.Ordinal);
        Assert.DoesNotContain("new CapabilityIdentity", world, StringComparison.Ordinal);
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
