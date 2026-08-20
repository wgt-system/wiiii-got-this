namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasReducedMotionContractTests
{
    [Fact]
    public void Appearance_surface_exposes_persistent_reduced_motion_and_applies_a_root_state_class()
    {
        var root = FindRepositoryRoot();
        var shell = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "ShellViewModel.AtlasMotion.cs"));
        var experience = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.Experience.cs"));

        Assert.Contains("AtlasMotionPreference.Reduced", shell, StringComparison.Ordinal);
        Assert.Contains("SetMotionAsync", shell, StringComparison.Ordinal);
        Assert.Contains("Toggle reduced motion", experience, StringComparison.Ordinal);
        Assert.Contains("SetStateClass(AtlasViewport, \"reduced-motion\", reducedMotion)", experience, StringComparison.Ordinal);
        Assert.Contains("motionPreferenceValue.Text = reducedMotion ? \"Reduced\" : \"Full\"", experience, StringComparison.Ordinal);
    }

    [Fact]
    public void Reduced_motion_removes_major_Atlas_and_product_transition_collections()
    {
        var root = FindRepositoryRoot();
        var styles = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "Styles",
            "AtlasReducedMotionStyles.axaml"));

        Assert.Contains("reduced-motion Border.wgt-atlas-node-shell", styles, StringComparison.Ordinal);
        Assert.Contains("reduced-motion Button.wgt-atlas-node", styles, StringComparison.Ordinal);
        Assert.Contains("reduced-motion Path.wgt-atlas-connection", styles, StringComparison.Ordinal);
        Assert.Contains("reduced-motion Border.wgt-node-emblem", styles, StringComparison.Ordinal);
        Assert.Contains("reduced-motion Grid.wgt-product-overlay", styles, StringComparison.Ordinal);
        Assert.Contains("Property=\"Transitions\" Value=\"{x:Null}\"", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void Reduced_motion_stops_visible_indeterminate_provider_loading_motion()
    {
        var root = FindRepositoryRoot();
        var reduced = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "Styles",
            "AtlasReducedMotionStyles.axaml"));
        var productStyles = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "Styles",
            "ProductSurfaceFinalStyles.axaml"));
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

        Assert.Contains("ProgressBar.wgt-provider-status-progress", productStyles, StringComparison.Ordinal);
        Assert.Contains("Property=\"IsIndeterminate\" Value=\"True\"", productStyles, StringComparison.Ordinal);
        Assert.Contains("reduced-motion ProgressBar.wgt-provider-status-progress", reduced, StringComparison.Ordinal);
        Assert.Contains("Property=\"IsIndeterminate\" Value=\"False\"", reduced, StringComparison.Ordinal);
        Assert.Contains("Property=\"Opacity\" Value=\"0\"", reduced, StringComparison.Ordinal);
        Assert.DoesNotContain("IsIndeterminate = true", vocation, StringComparison.Ordinal);
        Assert.DoesNotContain("IsIndeterminate = true", orientation, StringComparison.Ordinal);
        Assert.DoesNotContain("IsIndeterminate = true", productSurface, StringComparison.Ordinal);
    }

    [Fact]
    public void Provider_loading_state_uses_the_current_Atlas_theme_without_theming_provider_content()
    {
        var root = FindRepositoryRoot();
        var styles = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "Styles",
            "ProductSurfaceFinalStyles.axaml"));

        foreach (var theme in new[] { "technical", "elegant", "machine", "world" })
        {
            Assert.Contains($"theme-{theme} Border.wgt-provider-status-mark", styles, StringComparison.Ordinal);
            Assert.Contains($"theme-{theme} ProgressBar.wgt-provider-status-progress", styles, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("theme-technical Border.wgt-product-stage", styles, StringComparison.Ordinal);
        Assert.DoesNotContain("theme-elegant Border.wgt-product-stage", styles, StringComparison.Ordinal);
        Assert.DoesNotContain("theme-machine Border.wgt-product-stage", styles, StringComparison.Ordinal);
        Assert.DoesNotContain("theme-world Border.wgt-product-stage", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void Reduced_motion_does_not_leave_an_invisible_provider_return_delay()
    {
        var root = FindRepositoryRoot();
        var productSurface = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.ProductSurface.cs"));

        Assert.Contains("shell?.IsAtlasReducedMotion == true", productSurface, StringComparison.Ordinal);
        Assert.Contains("? 0", productSurface, StringComparison.Ordinal);
        Assert.Contains("if (delay > 0)", productSurface, StringComparison.Ordinal);
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
