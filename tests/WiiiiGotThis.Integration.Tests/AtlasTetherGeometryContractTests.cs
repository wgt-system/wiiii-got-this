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
    public void Inspector_keeps_user_owned_screen_position_while_tether_tracks_the_world_object()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.Polish.cs"));

        Assert.Contains("private bool inspectorHasPlacement;", source, StringComparison.Ordinal);
        Assert.Contains("private bool inspectorDragging;", source, StringComparison.Ordinal);
        Assert.Contains("inspectorDragOriginX", source, StringComparison.Ordinal);
        Assert.Contains("inspectorDragOriginY", source, StringComparison.Ordinal);
        Assert.Contains("e.Pointer.Capture(InspectorCard);", source, StringComparison.Ordinal);
        Assert.Contains("var left = inspectorTranslate.X;", source, StringComparison.Ordinal);
        Assert.Contains("var top = inspectorTranslate.Y;", source, StringComparison.Ordinal);
        Assert.DoesNotContain("preferLeft = nodeX < viewportCenter", source, StringComparison.Ordinal);
        Assert.DoesNotContain("leftCandidate = nodeX - nodeHalfWidth", source, StringComparison.Ordinal);
        Assert.DoesNotContain("rightCandidate = nodeX + nodeHalfWidth", source, StringComparison.Ordinal);
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
