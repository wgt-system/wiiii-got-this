namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasNodeCaptionVisualTests
{
    [Fact]
    public void Product_node_caption_masks_topology_without_restoring_a_full_card_shell()
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
            "AtlasContextualDetailStyles.axaml"));

        Assert.Contains("wgt-node-caption", renderer, StringComparison.Ordinal);
        Assert.Contains("Border.wgt-node-caption", styles, StringComparison.Ordinal);
        Assert.Contains("Button.wgt-atlas-node.theme-machine Border.wgt-node-caption", styles, StringComparison.Ordinal);
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
