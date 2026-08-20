namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasWorldEnvironmentalDetailContractTests
{
    [Fact]
    public void Flagship_grid_has_visual_depth_without_landscape_or_city_metaphors()
    {
        var root = FindRepositoryRoot();
        var grid = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasGridControl.cs"));
        var host = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.ProductionRenderer.cs"));

        Assert.Contains("DrawBackground", grid, StringComparison.Ordinal);
        Assert.Contains("DrawGrid", grid, StringComparison.Ordinal);
        Assert.Contains("DrawAmbientField", grid, StringComparison.Ordinal);
        Assert.Contains("DrawForegroundVignette", grid, StringComparison.Ordinal);
        Assert.Contains("GridMinor", grid, StringComparison.Ordinal);
        Assert.Contains("GridMajor", grid, StringComparison.Ordinal);
        Assert.Contains("GridPoint", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("LandOutline", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("DrawWater", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("DrawRoad", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("DrawRail", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("DrawVegetation", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("new AtlasWorldEnvironmentalDetailOverlay", host, StringComparison.Ordinal);
    }

    [Fact]
    public void Grid_geometry_is_viewport_bounded_and_camera_aware()
    {
        var root = FindRepositoryRoot();
        var grid = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasGridControl.cs"));

        Assert.Contains("leftWorld", grid, StringComparison.Ordinal);
        Assert.Contains("rightWorld", grid, StringComparison.Ordinal);
        Assert.Contains("topWorld", grid, StringComparison.Ordinal);
        Assert.Contains("bottomWorld", grid, StringComparison.Ordinal);
        Assert.Contains("Screen(new Point", grid, StringComparison.Ordinal);
        Assert.Contains("ClipToBounds = true", grid, StringComparison.Ordinal);
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
