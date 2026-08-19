namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasThemeIdentityVisualTests
{
    [Fact]
    public void Appearance_picker_uses_renderer_previews_instead_of_letter_badges()
    {
        var root = FindRepositoryRoot();
        var experience = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.Experience.cs"));

        Assert.Contains("BuildThemePreview", experience, StringComparison.Ordinal);
        Assert.Contains("PreviewRail", experience, StringComparison.Ordinal);
        Assert.Contains("PreviewRing", experience, StringComparison.Ordinal);
        Assert.Contains("PreviewDot", experience, StringComparison.Ordinal);
        Assert.DoesNotContain("ConfigureThemeChoice(TechnicalThemeButton, \"T\"", experience, StringComparison.Ordinal);
        Assert.DoesNotContain("ConfigureThemeChoice(WorldThemeButton, \"W\"", experience, StringComparison.Ordinal);
    }

    [Fact]
    public void Theme_renderers_have_distinct_node_bound_environment_languages()
    {
        var root = FindRepositoryRoot();
        var decorations = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.NodeDecorations.cs"));
        var styles = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "Styles",
            "AtlasThemeIdentityStyles.axaml"));

        Assert.Contains("AddTechnicalTicks", decorations, StringComparison.Ordinal);
        Assert.Contains("AddElegantHalo", decorations, StringComparison.Ordinal);
        Assert.Contains("AddMachineSockets", decorations, StringComparison.Ordinal);
        Assert.Contains("AddWorldLandmark", decorations, StringComparison.Ordinal);
        Assert.Contains("technical-tick.theme-technical", styles, StringComparison.Ordinal);
        Assert.Contains("elegant-halo.theme-elegant", styles, StringComparison.Ordinal);
        Assert.Contains("machine-socket.theme-machine", styles, StringComparison.Ordinal);
        Assert.Contains("world-landmark.theme-world", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void Theme_node_decorations_participate_in_focus_dimming()
    {
        var root = FindRepositoryRoot();
        var decorations = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.NodeDecorations.cs"));
        var experience = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.Experience.cs"));

        Assert.Contains("UpdateThemeNodeDecorationSelection", decorations, StringComparison.Ordinal);
        Assert.Contains("BuildFocusNodeSet", decorations, StringComparison.Ordinal);
        Assert.Contains("ApplyThemeToNodeDecorations", experience, StringComparison.Ordinal);
        Assert.Contains("RebuildThemeNodeDecorations", experience, StringComparison.Ordinal);
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
