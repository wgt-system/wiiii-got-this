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
