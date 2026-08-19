namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasCoreIdentityVisualTests
{
    [Fact]
    public void WGT_core_uses_a_composition_hub_vector_instead_of_text_as_its_primary_graphic()
    {
        var root = FindRepositoryRoot();
        var renderer = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.FinalRenderer.cs"));
        var factory = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "CoreSigilFactory.cs"));

        Assert.Contains("CoreSigilFactory.Create(64)", renderer, StringComparison.Ordinal);
        Assert.DoesNotContain("Text = \"WGT\"", renderer, StringComparison.Ordinal);
        Assert.Contains("wgt-core-sigil", factory, StringComparison.Ordinal);
        Assert.Contains("wgt-core-sigil-port", factory, StringComparison.Ordinal);
        Assert.Contains("new Point(24, 3)", factory, StringComparison.Ordinal);
        Assert.Contains("new Point(45, 24)", factory, StringComparison.Ordinal);
        Assert.Contains("new Point(24, 45)", factory, StringComparison.Ordinal);
        Assert.Contains("new Point(3, 24)", factory, StringComparison.Ordinal);
    }

    [Fact]
    public void Core_identity_is_rendered_by_all_four_Atlas_visual_languages()
    {
        var root = FindRepositoryRoot();
        var styles = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "Styles",
            "AtlasCoreIdentityStyles.axaml"));
        var app = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "App.axaml"));

        foreach (var theme in new[] { "technical", "elegant", "machine", "world" })
        {
            Assert.Contains($"core.theme-{theme} Path.wgt-core-sigil", styles, StringComparison.Ordinal);
            Assert.Contains($"core.theme-{theme} Border.wgt-core-sigil-port", styles, StringComparison.Ordinal);
        }

        Assert.Contains("AtlasCoreIdentityStyles.axaml", app, StringComparison.Ordinal);
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
