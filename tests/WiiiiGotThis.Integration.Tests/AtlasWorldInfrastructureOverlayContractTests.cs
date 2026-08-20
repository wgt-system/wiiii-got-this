namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasWorldInfrastructureOverlayContractTests
{
    [Fact]
    public void Shared_capability_consumption_is_projected_as_selected_relationship_traces_and_provider_infrastructure()
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

        Assert.Contains("DrawInfrastructureBus", grid, StringComparison.Ordinal);
        Assert.Contains("DrawInfrastructureNode", grid, StringComparison.Ordinal);
        Assert.Contains("DrawRelationshipTraces", grid, StringComparison.Ordinal);
        Assert.Contains("connection.IsEnabled", grid, StringComparison.Ordinal);
        Assert.Contains("connection.IsCapabilityUse", grid, StringComparison.Ordinal);
        Assert.Contains("DrawOrthogonalTrace", grid, StringComparison.Ordinal);
        Assert.Contains("DrawVisibleCapabilityPorts", grid, StringComparison.Ordinal);
        Assert.Contains("atlasGridRenderer = new AtlasGridControl", host, StringComparison.Ordinal);
        Assert.DoesNotContain("new AtlasWorldInfrastructureOverlay", host, StringComparison.Ordinal);
    }

    [Fact]
    public void Infrastructure_visualization_does_not_invent_domain_entities_or_peer_products()
    {
        var root = FindRepositoryRoot();
        var grid = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasGridControl.cs"));

        Assert.Contains("node.IsSharedCapabilityProvider", grid, StringComparison.Ordinal);
        Assert.Contains("AtlasConnectionKind.CapabilityOwnership", grid, StringComparison.Ordinal);
        Assert.Contains("connection.IsCapabilityUse", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("new ServiceIdentity", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("new CapabilityIdentity", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("facility", grid, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("warehouse", grid, StringComparison.OrdinalIgnoreCase);
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
