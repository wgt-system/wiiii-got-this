namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasContextualCapabilityVisualTests
{
    [Fact]
    public void Capabilities_are_compact_ports_until_their_focus_context_is_active()
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

        Assert.Contains("BuildCapabilityDot", renderer, StringComparison.Ordinal);
        Assert.Contains("detailContext && focusNodeIds.Contains(node.NodeId)", renderer, StringComparison.Ordinal);
        Assert.Contains("nodeShell.Width = 32", renderer, StringComparison.Ordinal);
        Assert.Contains("nodeShell.Width = 160", renderer, StringComparison.Ordinal);
        Assert.Contains("compact-port", styles, StringComparison.Ordinal);
        Assert.Contains("wgt-node-compact-port-dot", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void Service_emblems_use_provider_specific_vector_sigils_instead_of_font_placeholders()
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

        Assert.Contains("BuildServiceSigil", renderer, StringComparison.Ordinal);
        Assert.Contains("BuildServiceSigilGeometry", renderer, StringComparison.Ordinal);
        Assert.Contains("case \"Vocation\"", renderer, StringComparison.Ordinal);
        Assert.Contains("case \"Illumination\"", renderer, StringComparison.Ordinal);
        Assert.Contains("case \"Orientation\"", renderer, StringComparison.Ordinal);
        Assert.Contains("case \"Conveyance\"", renderer, StringComparison.Ordinal);
        Assert.Contains("Path.wgt-service-sigil", styles, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Vocation\" => \"↗\"", renderer, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Illumination\" => \"✦\"", renderer, StringComparison.Ordinal);
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
