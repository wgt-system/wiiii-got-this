namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasOverviewCameraContractTests
{
    [Fact]
    public void Overview_camera_fits_core_and_first_class_services_into_safe_desktop_chrome()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.OverviewCamera.cs"));

        Assert.Contains("node.IsCore || node.IsService", source, StringComparison.Ordinal);
        Assert.Contains("topSafeArea = 104d", source, StringComparison.Ordinal);
        Assert.Contains("bottomSafeArea = 42d", source, StringComparison.Ordinal);
        Assert.Contains("horizontalSafeArea = 72d", source, StringComparison.Ordinal);
        Assert.Contains("0.66d", source, StringComparison.Ordinal);
        Assert.Contains("1.08d", source, StringComparison.Ordinal);
        Assert.Contains("targetScreenY = topSafeArea + availableHeight / 2d", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Center_WGT_uses_fit_to_system_and_only_falls_back_to_legacy_reset()
    {
        var root = FindRepositoryRoot();
        var navigation = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.NavigationChrome.cs"));

        Assert.Contains("Fit WGT Atlas", navigation, StringComparison.Ordinal);
        Assert.Contains("if (!FitOverviewCamera())", navigation, StringComparison.Ordinal);
        Assert.Contains("ResetCamera();", navigation, StringComparison.Ordinal);
        Assert.Contains("QueueInitialOverviewFit();", navigation, StringComparison.Ordinal);
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
