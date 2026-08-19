namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasOverviewVisualContractTests
{
    [Fact]
    public void Overview_declutters_capability_detail_until_a_node_is_selected()
    {
        var root = FindRepositoryRoot();
        var renderer = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.FinalRenderer.cs"));
        var styles = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "Styles",
            "AtlasInteractionFinalStyles.axaml"));

        Assert.Contains("overview && node.IsCapability", renderer, StringComparison.Ordinal);
        Assert.Contains("connection.Kind != AtlasConnectionKind.Composition", renderer, StringComparison.Ordinal);
        Assert.Contains("capability.overview-secondary", styles, StringComparison.Ordinal);
        Assert.Contains("Path.wgt-atlas-connection.overview-secondary", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void Desktop_window_requests_dark_native_appearance_for_the_immersive_atlas()
    {
        var root = FindRepositoryRoot();
        var window = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "MainWindow.axaml"));

        Assert.Contains("RequestedThemeVariant=\"Dark\"", window, StringComparison.Ordinal);
        Assert.Contains("Title=\"Wiiii Got This · Atlas\"", window, StringComparison.Ordinal);
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
