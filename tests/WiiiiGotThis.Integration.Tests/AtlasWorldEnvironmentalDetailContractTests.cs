namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasWorldEnvironmentalDetailContractTests
{
    [Fact]
    public void World_adds_authored_environmental_structure_beyond_settlement_blobs()
    {
        var root = FindRepositoryRoot();
        var overlay = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasWorldEnvironmentalDetailOverlay.cs"));
        var host = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.ProductionRenderer.cs"));

        Assert.Contains("DrawVocationFarmland", overlay, StringComparison.Ordinal);
        Assert.Contains("DrawVocationOrchard", overlay, StringComparison.Ordinal);
        Assert.Contains("DrawIlluminationTerraces", overlay, StringComparison.Ordinal);
        Assert.Contains("DrawIlluminationGarden", overlay, StringComparison.Ordinal);
        Assert.Contains("DrawWgtGreenBelt", overlay, StringComparison.Ordinal);
        Assert.Contains("DrawOrientationContours", overlay, StringComparison.Ordinal);
        Assert.Contains("DrawOrientationTrail", overlay, StringComparison.Ordinal);
        Assert.Contains("DrawTributary", overlay, StringComparison.Ordinal);
        Assert.Contains("DrawConveyanceRail", overlay, StringComparison.Ordinal);
        Assert.Contains("DrawAuthoredTreeMasses", overlay, StringComparison.Ordinal);
        Assert.Contains("new AtlasWorldEnvironmentalDetailOverlay", host, StringComparison.Ordinal);
        Assert.Contains("livingWorldEnvironmentalOverlay.IsVisible = isLivingWorld", host, StringComparison.Ordinal);
    }

    [Fact]
    public void World_environmental_geometry_uses_concrete_point_arrays_on_the_hot_render_path()
    {
        var root = FindRepositoryRoot();
        var overlay = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasWorldEnvironmentalDetailOverlay.cs"));

        Assert.Contains("DrawField(DrawingContext context, Point[] points", overlay, StringComparison.Ordinal);
        Assert.Contains("PolygonWorld(Point[] points)", overlay, StringComparison.Ordinal);
        Assert.Contains("SmoothWorldPath(Point[] points)", overlay, StringComparison.Ordinal);
        Assert.Contains("BoundsOf(Point[] points)", overlay, StringComparison.Ordinal);
        Assert.DoesNotContain("IReadOnlyList<Point> points", overlay, StringComparison.Ordinal);
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
