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
        var styles = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "Styles",
            "ProductSurfaceFinalStyles.axaml"));

        Assert.Contains("Return to WGT Atlas", source, StringComparison.Ordinal);
        Assert.Contains("WGT settings", source, StringComparison.Ordinal);
        Assert.Contains("BuildProviderCapabilityRail", source, StringComparison.Ordinal);
        Assert.Contains("capabilityScroll", source, StringComparison.Ordinal);
        Assert.Contains("VerticalScrollBarVisibility", source, StringComparison.Ordinal);
        Assert.Contains("ProductRailCapability", source, StringComparison.Ordinal);
        Assert.Contains("OnProductRailCapability", source, StringComparison.Ordinal);
        Assert.Contains("returnButton.Classes.Add(\"wgt-product-rail-global\")", source, StringComparison.Ordinal);
        Assert.Contains("settingsButton.Classes.Add(\"wgt-product-rail-global\")", source, StringComparison.Ordinal);
        Assert.Contains("button.Classes.Add(\"wgt-product-rail-provider\")", source, StringComparison.Ordinal);
        Assert.Contains("button.Classes.Add(\"wgt-product-rail-toggle\")", source, StringComparison.Ordinal);
        Assert.Contains("rail.Classes.Add(serviceName.ToLowerInvariant())", source, StringComparison.Ordinal);
        Assert.Contains("Button.wgt-product-rail-global", styles, StringComparison.Ordinal);
        Assert.Contains("Button.wgt-product-rail-provider", styles, StringComparison.Ordinal);
        Assert.Contains("Button.wgt-product-rail-toggle", styles, StringComparison.Ordinal);
        Assert.Contains("Border.wgt-product-rail.vocation Button.wgt-product-rail-provider", styles, StringComparison.Ordinal);
        Assert.Contains("Border.wgt-product-rail.illumination Button.wgt-product-rail-provider", styles, StringComparison.Ordinal);
        Assert.Contains("Border.wgt-product-rail.orientation Button.wgt-product-rail-provider", styles, StringComparison.Ordinal);
        Assert.DoesNotContain("FULL PRODUCT", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Provider_rail_uses_curated_Atlas_relationships_instead_of_raw_integration_publications()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.ProductSurface.cs"));

        Assert.Contains("shell.AtlasConnections", source, StringComparison.Ordinal);
        Assert.Contains("connection.IsCapabilityUse", source, StringComparison.Ordinal);
        Assert.Contains("connection.Source.ServiceIdentity", source, StringComparison.Ordinal);
        Assert.Contains("shell.AtlasNodes", source, StringComparison.Ordinal);
        Assert.Contains("node.IsCapability", source, StringComparison.Ordinal);
        Assert.Contains("BuildAtlasProjectionUseCase.OrientationGeospatialCapabilityId", source, StringComparison.Ordinal);
        Assert.Contains("BuildAtlasProjectionUseCase.ConveyanceDurableDeliveryCapabilityId", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectedIntegrationCapabilities", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Configurable_product_capability_use_can_be_toggled_without_leaving_the_product_surface()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.ProductSurface.cs"));
        var shell = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "ShellViewModel.CapabilityConsumption.cs"));

        Assert.Contains("connection.IsUserConfigurable ? OnProductRailCapabilityToggle", source, StringComparison.Ordinal);
        Assert.Contains("connection.StateText", source, StringComparison.Ordinal);
        Assert.Contains("button.Opacity = connection.IsEnabled ? 1 : 0.42", source, StringComparison.Ordinal);
        Assert.Contains("shell.ToggleCapabilityConsumptionAsync(connection)", source, StringComparison.Ordinal);
        Assert.Contains("button.Opacity = enabled ? 1 : 0.42", source, StringComparison.Ordinal);
        Assert.Contains("writeCapabilityConsumption.SetAsync", shell, StringComparison.Ordinal);
        Assert.Contains("buildAtlasProjection.SetConsumptionPreferences(updated)", shell, StringComparison.Ordinal);
    }

    [Fact]
    public void Navigation_rail_actions_return_to_Atlas_context_before_selecting_WGT_controls()
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
