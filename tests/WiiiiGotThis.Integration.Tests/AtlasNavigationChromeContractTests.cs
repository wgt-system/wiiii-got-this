namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasNavigationChromeContractTests
{
    [Fact]
    public void Atlas_exposes_a_small_center_WGT_affordance_instead_of_persistent_instruction_chrome()
    {
        var root = FindRepositoryRoot();
        var navigation = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.NavigationChrome.cs"));
        var polish = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.Polish.cs"));
        var finalRenderer = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.FinalRenderer.cs"));

        Assert.Contains("AtlasCenterWgt", navigation, StringComparison.Ordinal);
        Assert.Contains("Center WGT Atlas", navigation, StringComparison.Ordinal);
        Assert.Contains("ResetCamera();", navigation, StringComparison.Ordinal);
        Assert.Contains("SelectAtlasNodeCommand.Execute(null)", navigation, StringComparison.Ordinal);
        Assert.Contains("EnsureAtlasNavigationChrome();", polish, StringComparison.Ordinal);
        Assert.Contains("ControlHint.IsVisible = false", finalRenderer, StringComparison.Ordinal);
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
