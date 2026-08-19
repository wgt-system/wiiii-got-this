namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasTetherGeometryContractTests
{
    [Fact]
    public void Inspector_tether_matches_the_final_node_and_inspector_geometry()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.Polish.cs"));

        Assert.Contains("372d", source, StringComparison.Ordinal);
        Assert.Contains("AtlasNodeKind.Core => 94d", source, StringComparison.Ordinal);
        Assert.Contains("AtlasNodeKind.Service => 74d", source, StringComparison.Ordinal);
        Assert.Contains("_ => 80d", source, StringComparison.Ordinal);
        Assert.DoesNotContain("404d", source, StringComparison.Ordinal);
        Assert.DoesNotContain("_ => 31d", source, StringComparison.Ordinal);
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
