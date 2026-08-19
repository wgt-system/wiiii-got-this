namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasWorldV2ContractTests
{
    [Fact]
    public void World_v2_is_a_contiguous_authored_landscape_not_the_old_settlement_blob_graph()
    {
        var root = FindRepositoryRoot();
        var world = File.ReadAllText(Path.Combine(root, "src", "WiiiiGotThis.Presentation", "AtlasWorldV2Control.cs"));
        var host = File.ReadAllText(Path.Combine(root, "src", "WiiiiGotThis.Presentation", "DesktopAtlasView.ProductionRenderer.cs"));

        Assert.Contains("MainLand", world, StringComparison.Ordinal);
        Assert.Contains("AuthoredServicePositions", world, StringComparison.Ordinal);
        Assert.Contains("AdditionalProductSlots", world, StringComparison.Ordinal);
        Assert.Contains("DrawRiver", world, StringComparison.Ordinal);
        Assert.Contains("DrawPrimaryRoads", world, StringComparison.Ordinal);
        Assert.Contains("DrawRailAndLogistics", world, StringComparison.Ordinal);
        Assert.Contains("DrawWgtCity", world, StringComparison.Ordinal);
        Assert.Contains("DrawVocation", world, StringComparison.Ordinal);
        Assert.Contains("DrawIllumination", world, StringComparison.Ordinal);
        Assert.Contains("DrawOrientation", world, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateOrganicGroundPatch", world, StringComparison.Ordinal);
        Assert.DoesNotContain("COMPOSED", world, StringComparison.Ordinal);
        Assert.DoesNotContain("Opportunity Overview", world, StringComparison.Ordinal);
        Assert.DoesNotContain("Map Projection", world, StringComparison.Ordinal);

        Assert.Contains("worldV2Renderer = new AtlasWorldV2Control", host, StringComparison.Ordinal);
        Assert.DoesNotContain("new AtlasLivingWorldControl", host, StringComparison.Ordinal);
        Assert.DoesNotContain("new AtlasWorldRegionalArchitectureOverlay", host, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_capabilities_become_local_facilities_and_progressive_networks()
    {
        var root = FindRepositoryRoot();
        var world = File.ReadAllText(Path.Combine(root, "src", "WiiiiGotThis.Presentation", "AtlasWorldV2Control.cs"));

        Assert.Contains("DrawConsumerFacility", world, StringComparison.Ordinal);
        Assert.Contains("BuildAtlasProjectionUseCase.ConveyanceDurableDeliveryCapabilityId", world, StringComparison.Ordinal);
        Assert.Contains("BuildAtlasProjectionUseCase.OrientationGeospatialCapabilityId", world, StringComparison.Ordinal);
        Assert.Contains("DrawSemanticNetworks", world, StringComparison.Ordinal);
        Assert.Contains("zoom < CloseDetailZoom && !focusedRoute", world, StringComparison.Ordinal);
        Assert.Contains("connection.IsEnabled", world, StringComparison.Ordinal);
        Assert.DoesNotContain("KindLabel", world, StringComparison.Ordinal);
        Assert.DoesNotContain("CompactStateText", world, StringComparison.Ordinal);
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

        throw new DirectoryNotFoundException("Could not locate the Wiiii Got This repository root.");
    }
}
