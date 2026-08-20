namespace WiiiiGotThis.Integration.Tests;

public sealed class ProductSurfaceVisualContractTests
{
    [Fact]
    public void Provider_entry_uses_a_narrow_depth_rail_instead_of_a_full_navigation_shell()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.ProductSurface.cs"));

        Assert.Contains("ColumnDefinitions = new ColumnDefinitions(\"68,*\")", source, StringComparison.Ordinal);
        Assert.Contains("wgt-product-depth-track", source, StringComparison.Ordinal);
        Assert.Contains("Return to WGT Atlas", source, StringComparison.Ordinal);
        Assert.Contains("WGT settings", source, StringComparison.Ordinal);
        Assert.Contains("BuildProviderCapabilityRail", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FULL PRODUCT", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ColumnDefinitions = new ColumnDefinitions(\"76,*\")", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Provider_startup_states_reuse_the_same_vector_service_identity_as_the_atlas()
    {
        var root = FindRepositoryRoot();
        var vocation = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "VocationProductView.cs"));
        var orientation = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "OrientationProductView.cs"));
        var productSurface = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.ProductSurface.cs"));

        Assert.Contains("ServiceSigilFactory.Create(\"Vocation\", 42)", vocation, StringComparison.Ordinal);
        Assert.Contains("wgt-provider-status-panel", vocation, StringComparison.Ordinal);
        Assert.Contains("ServiceSigilFactory.Create(\"Orientation\", 42)", orientation, StringComparison.Ordinal);
        Assert.Contains("wgt-provider-status-panel", orientation, StringComparison.Ordinal);
        Assert.Contains("ServiceSigilFactory.Create(\"Illumination\", 42)", productSurface, StringComparison.Ordinal);
        Assert.Contains("ServiceSigilFactory.Create(serviceName, 28)", productSurface, StringComparison.Ordinal);
        Assert.Contains("wgt-provider-status-panel", productSurface, StringComparison.Ordinal);
        Assert.DoesNotContain("Text = \"VO\"", vocation, StringComparison.Ordinal);
        Assert.DoesNotContain("Text = \"OR\"", orientation, StringComparison.Ordinal);
        Assert.DoesNotContain("Text = \"IL\"", productSurface, StringComparison.Ordinal);
    }

    [Fact]
    public void Atlas_themes_continue_only_through_WGT_owned_product_depth_chrome()
    {
        var root = FindRepositoryRoot();
        var styles = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "Styles",
            "ProductSurfaceFinalStyles.axaml"));

        Assert.Contains("Button.wgt-product-rail-action", styles, StringComparison.Ordinal);
        Assert.Contains("Button.wgt-product-rail-action:pointerover", styles, StringComparison.Ordinal);
        Assert.Contains("Button.wgt-product-rail-action:focus-visible", styles, StringComparison.Ordinal);

        foreach (var theme in new[] { "technical", "elegant", "machine", "world" })
        {
            Assert.Contains($"theme-{theme} Border.wgt-product-rail", styles, StringComparison.Ordinal);
            Assert.Contains($"theme-{theme} Border.wgt-product-depth-track", styles, StringComparison.Ordinal);
            Assert.Contains($"theme-{theme} Button.wgt-product-rail-action", styles, StringComparison.Ordinal);
            Assert.Contains($"theme-{theme} Button.wgt-product-return", styles, StringComparison.Ordinal);
        }

        Assert.Contains("theme-machine Border.wgt-product-service-mark", styles, StringComparison.Ordinal);
        Assert.Contains("theme-world Border.wgt-product-service-mark", styles, StringComparison.Ordinal);
        Assert.DoesNotContain("theme-technical Border.wgt-product-stage", styles, StringComparison.Ordinal);
        Assert.DoesNotContain("theme-elegant Border.wgt-product-stage", styles, StringComparison.Ordinal);
        Assert.DoesNotContain("theme-machine Border.wgt-product-stage", styles, StringComparison.Ordinal);
        Assert.DoesNotContain("theme-world Border.wgt-product-stage", styles, StringComparison.Ordinal);
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
