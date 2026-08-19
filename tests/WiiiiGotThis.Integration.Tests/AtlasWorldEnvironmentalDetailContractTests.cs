namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasWorldEnvironmentalDetailContractTests
{
    [Fact]
    public void World_v2_contains_authored_environmental_structure_beyond_settlement_blobs()
    {
        var root = FindRepositoryRoot();
        var world = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasWorldV2Control.cs"));
        var host = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.ProductionRenderer.cs"));

        Assert.Contains("DrawContiguousTerrain", world, StringComparison.Ordinal);
        Assert.Contains("DrawRegionalLandUse", world, StringComparison.Ordinal);
        Assert.Contains("DrawWater", world, StringComparison.Ordinal);
        Assert.Contains("DrawRoadNetwork", world, StringComparison.Ordinal);
        Assert.Contains("DrawRailNetwork", world, StringComparison.Ordinal);
        Assert.Contains("DrawVegetation", world, StringComparison.Ordinal);
        Assert.Contains("vocationFields", world, StringComparison.Ordinal);
        Assert.Contains("illuminationGarden", world, StringComparison.Ordinal);
        Assert.Contains("orientationRidge", world, StringComparison.Ordinal);
        Assert.Contains("wgtGreenbelt", world, StringComparison.Ordinal);
        Assert.Contains("tributary", world, StringComparison.Ordinal);
        Assert.DoesNotContain("new AtlasWorldEnvironmentalDetailOverlay", host, StringComparison.Ordinal);
    }

    [Fact]
    public void World_v2_environmental_geometry_uses_concrete_point_arrays_on_the_hot_render_path()
    {
        var root = FindRepositoryRoot();
        var world = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasWorldV2Control.cs"));

        Assert.Contains("Point[] trees", world, StringComparison.Ordinal);
        Assert.Contains("ClosedWorldShape(Point[] worldPoints", world, StringComparison.Ordinal);
        Assert.Contains("OpenWorldRoute(Point[] worldPoints)", world, StringComparison.Ordinal);
        Assert.Contains("DrawRoad(DrawingContext context, Point[] points", world, StringComparison.Ordinal);
        Assert.DoesNotContain("IReadOnlyList<Point> points", world, StringComparison.Ordinal);
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
