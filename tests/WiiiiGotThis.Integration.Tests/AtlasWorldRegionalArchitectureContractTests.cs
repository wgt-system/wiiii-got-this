namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasWorldRegionalArchitectureContractTests
{
    [Fact]
    public void World_layers_distinct_built_environment_identity_over_current_product_regions()
    {
        var root = FindRepositoryRoot();
        var overlay = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasWorldRegionalArchitectureOverlay.cs"));
        var host = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.ProductionRenderer.cs"));

        Assert.Contains("DrawVocationQuarter", overlay, StringComparison.Ordinal);
        Assert.Contains("DrawIlluminationCampus", overlay, StringComparison.Ordinal);
        Assert.Contains("DrawOrientationSurveyDistrict", overlay, StringComparison.Ordinal);
        Assert.Contains("DrawLanternCrown", overlay, StringComparison.Ordinal);
        Assert.Contains("DrawSurveyMast", overlay, StringComparison.Ordinal);
        Assert.Contains("DrawCourtyard", overlay, StringComparison.Ordinal);
        Assert.Contains("DrawExtrudedBlock", overlay, StringComparison.Ordinal);
        Assert.Contains("new AtlasWorldRegionalArchitectureOverlay", host, StringComparison.Ordinal);
        Assert.Contains("livingWorldRegionalArchitectureOverlay.IsVisible = isLivingWorld", host, StringComparison.Ordinal);
        Assert.Contains("livingWorldRegionalArchitectureOverlay.SetScene", host, StringComparison.Ordinal);
        Assert.Contains("livingWorldRegionalArchitectureOverlay?.SetCamera", host, StringComparison.Ordinal);
    }

    [Fact]
    public void Authored_regional_landmarks_remain_presentation_only_and_non_interactive()
    {
        var root = FindRepositoryRoot();
        var overlay = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasWorldRegionalArchitectureOverlay.cs"));

        Assert.Contains("IsHitTestVisible = false", overlay, StringComparison.Ordinal);
        Assert.Contains("node.IsService", overlay, StringComparison.Ordinal);
        Assert.DoesNotContain("NodeInvoked", overlay, StringComparison.Ordinal);
        Assert.DoesNotContain("NodeActivated", overlay, StringComparison.Ordinal);
        Assert.DoesNotContain("CapabilityIdentity", overlay, StringComparison.Ordinal);
        Assert.DoesNotContain("ServiceIdentity(", overlay, StringComparison.Ordinal);
        Assert.Contains("PolygonWorld(Point[] points)", overlay, StringComparison.Ordinal);
    }

    [Fact]
    public void Authored_landmark_centers_stay_aligned_with_the_current_living_world_layout()
    {
        var root = FindRepositoryRoot();
        var overlay = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasWorldRegionalArchitectureOverlay.cs"));
        var renderer = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasLivingWorldControl.cs"));

        foreach (var coordinate in new[]
                 {
                     "new(-410, 80)",
                     "new(-115, -350)",
                     "new(405, -28)"
                 })
        {
            Assert.Contains(coordinate, overlay, StringComparison.Ordinal);
        }

        Assert.Contains("new Point(-410, 80)", renderer, StringComparison.Ordinal);
        Assert.Contains("new Point(-115, -350)", renderer, StringComparison.Ordinal);
        Assert.Contains("new Point(405, -28)", renderer, StringComparison.Ordinal);
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
