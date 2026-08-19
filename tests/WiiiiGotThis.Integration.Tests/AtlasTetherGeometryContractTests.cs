namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasTetherGeometryContractTests
{
    [Fact]
    public void Inspector_tether_matches_the_compact_spatial_dossier_and_final_node_geometry()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.Polish.cs"));

        Assert.Contains("InspectorCard.Width = 300", source, StringComparison.Ordinal);
        Assert.Contains("InspectorCard.MaxHeight = 560", source, StringComparison.Ordinal);
        Assert.Contains("300d", source, StringComparison.Ordinal);
        Assert.Contains("AtlasNodeKind.Core => 94d", source, StringComparison.Ordinal);
        Assert.Contains("AtlasNodeKind.Service => 74d", source, StringComparison.Ordinal);
        Assert.Contains("_ => 80d", source, StringComparison.Ordinal);
        Assert.DoesNotContain("372d", source, StringComparison.Ordinal);
        Assert.DoesNotContain("404d", source, StringComparison.Ordinal);
        Assert.DoesNotContain("_ => 31d", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Inspector_prefers_the_outward_side_of_the_selected_service()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.Polish.cs"));

        Assert.Contains("preferLeft = nodeX < viewportCenter - 50d", source, StringComparison.Ordinal);
        Assert.Contains("leftCandidate = nodeX - nodeHalfWidth - gap - cardWidth", source, StringComparison.Ordinal);
        Assert.Contains("rightCandidate = nodeX + nodeHalfWidth + gap", source, StringComparison.Ordinal);
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
