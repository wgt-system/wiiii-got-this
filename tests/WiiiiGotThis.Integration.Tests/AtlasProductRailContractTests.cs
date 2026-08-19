namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasProductRailContractTests
{
    [Fact]
    public void Hosted_product_rail_separates_WGT_controls_from_provider_capability_controls()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.ProductSurface.cs"));

        Assert.Contains("Return to WGT Atlas", source, StringComparison.Ordinal);
        Assert.Contains("WGT settings", source, StringComparison.Ordinal);
        Assert.Contains("BuildProviderCapabilityRail", source, StringComparison.Ordinal);
        Assert.Contains("capabilityScroll", source, StringComparison.Ordinal);
        Assert.Contains("VerticalScrollBarVisibility", source, StringComparison.Ordinal);
        Assert.Contains("ProductRailCapability", source, StringComparison.Ordinal);
        Assert.Contains("OnProductRailCapability", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FULL PRODUCT", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Provider_rail_uses_curated_Atlas_capabilities_instead_of_raw_integration_publications()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.ProductSurface.cs"));

        Assert.Contains("shell?.AtlasNodes", source, StringComparison.Ordinal);
        Assert.Contains("node.IsCapability", source, StringComparison.Ordinal);
        Assert.Contains("BuildAtlasProjectionUseCase.OrientationGeospatialCapabilityId", source, StringComparison.Ordinal);
        Assert.Contains("BuildAtlasProjectionUseCase.ConveyanceDurableDeliveryCapabilityId", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectedIntegrationCapabilities", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Rail_actions_return_to_Atlas_context_before_selecting_WGT_controls()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.ProductSurface.cs"));

        Assert.Contains("await HideActiveProductOverlayAsync();", source, StringComparison.Ordinal);
        Assert.Contains("shell.AtlasSettingsExpanded = true;", source, StringComparison.Ordinal);
        Assert.Contains("shell.SelectAtlasNodeCommand.Execute(capability);", source, StringComparison.Ordinal);
        Assert.Contains("CenterOnSelected();", source, StringComparison.Ordinal);
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
