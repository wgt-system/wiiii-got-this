namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasConnectionVisualContractTests
{
    [Fact]
    public void Connections_are_surface_anchored_instead_of_center_to_center()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.ConnectionRenderer.cs"));

        Assert.Contains("ConnectionAnchorDistance(connection.Source)", source, StringComparison.Ordinal);
        Assert.Contains("ConnectionAnchorDistance(connection.Target)", source, StringComparison.Ordinal);
        Assert.Contains("AtlasNodeKind.Core => 94d", source, StringComparison.Ordinal);
        Assert.Contains("AtlasNodeKind.Service => 74d", source, StringComparison.Ordinal);
        Assert.Contains("AtlasNodeKind.Capability => 16d", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Capability_dependencies_receive_a_directional_endpoint_marker()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.ConnectionRenderer.cs"));

        Assert.Contains("AtlasConnectionKind.CapabilityDependency", source, StringComparison.Ordinal);
        Assert.Contains("AddDependencyArrowHead", source, StringComparison.Ordinal);
        Assert.Contains("context.LineTo(end)", source, StringComparison.Ordinal);
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
