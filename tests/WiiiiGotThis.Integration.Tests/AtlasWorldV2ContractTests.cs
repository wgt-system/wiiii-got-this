namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasWorldV2ContractTests
{
    [Fact]
    public void Flagship_atlas_is_an_abstract_modular_grid_not_a_city_or_settlement_renderer()
    {
        var root = FindRepositoryRoot();
        var grid = File.ReadAllText(Path.Combine(root, "src", "WiiiiGotThis.Presentation", "AtlasGridControl.cs"));
        var host = File.ReadAllText(Path.Combine(root, "src", "WiiiiGotThis.Presentation", "DesktopAtlasView.ProductionRenderer.cs"));

        Assert.Contains("ProductSlot", grid, StringComparison.Ordinal);
        Assert.Contains("DrawGrid", grid, StringComparison.Ordinal);
        Assert.Contains("DrawAmbientField", grid, StringComparison.Ordinal);
        Assert.Contains("DrawPrimaryNodes", grid, StringComparison.Ordinal);
        Assert.Contains("DrawInfrastructureBus", grid, StringComparison.Ordinal);
        Assert.Contains("DrawRelationshipTraces", grid, StringComparison.Ordinal);
        Assert.Contains("DrawVisibleCapabilityPorts", grid, StringComparison.Ordinal);
        Assert.Contains("PreferredProductOrder", grid, StringComparison.Ordinal);
        Assert.Contains("maxColumns = 4", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("DrawWgtCity", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("DrawVocation", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("DrawIllumination", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("DrawOrientation", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("DrawConveyance", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("LandOutline", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("COMPOSED", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("Opportunity Overview", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("Map Projection", grid, StringComparison.Ordinal);

        Assert.Contains("atlasGridRenderer = new AtlasGridControl", host, StringComparison.Ordinal);
        Assert.DoesNotContain("new AtlasWorldV2Control", host, StringComparison.Ordinal);
        Assert.DoesNotContain("new AtlasLivingWorldControl", host, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_capability_providers_use_a_distinct_infrastructure_layer_and_capability_ports()
    {
        var root = FindRepositoryRoot();
        var grid = File.ReadAllText(Path.Combine(root, "src", "WiiiiGotThis.Presentation", "AtlasGridControl.cs"));

        Assert.Contains("node.IsSharedCapabilityProvider", grid, StringComparison.Ordinal);
        Assert.Contains("DrawInfrastructureNode", grid, StringComparison.Ordinal);
        Assert.Contains("DrawInfrastructureBus", grid, StringComparison.Ordinal);
        Assert.Contains("AtlasConnectionKind.CapabilityOwnership", grid, StringComparison.Ordinal);
        Assert.Contains("connection.IsCapabilityUse", grid, StringComparison.Ordinal);
        Assert.Contains("connection.IsEnabled", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("KindLabel", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("CompactStateText", grid, StringComparison.Ordinal);
    }

    [Fact]
    public void Grid_exposes_deterministic_node_anchors_for_dossier_and_navigation_alignment()
    {
        var root = FindRepositoryRoot();
        var grid = File.ReadAllText(Path.Combine(root, "src", "WiiiiGotThis.Presentation", "AtlasGridControl.cs"));
        var polish = File.ReadAllText(Path.Combine(root, "src", "WiiiiGotThis.Presentation", "DesktopAtlasView.Polish.cs"));

        Assert.Contains("TryGetWorldPosition", grid, StringComparison.Ordinal);
        Assert.Contains("nodePlaces.TryGetValue", grid, StringComparison.Ordinal);
        Assert.Contains("atlasGridRenderer.TryGetWorldPosition", polish, StringComparison.Ordinal);
    }

    [Fact]
    public void Pointer_and_keyboard_feedback_are_immediate_animated_and_reduced_motion_aware()
    {
        var root = FindRepositoryRoot();
        var grid = File.ReadAllText(Path.Combine(root, "src", "WiiiiGotThis.Presentation", "AtlasGridControl.cs"));

        Assert.Contains("OnPointerMoved", grid, StringComparison.Ordinal);
        Assert.Contains("OnPointerPressed", grid, StringComparison.Ordinal);
        Assert.Contains("OnPointerReleased", grid, StringComparison.Ordinal);
        Assert.Contains("UpdateInteraction", grid, StringComparison.Ordinal);
        Assert.Contains("RequestAnimationFrame", grid, StringComparison.Ordinal);
        Assert.Contains("MoveSelection", grid, StringComparison.Ordinal);
        Assert.Contains("if (reducedMotion)", grid, StringComparison.Ordinal);
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
