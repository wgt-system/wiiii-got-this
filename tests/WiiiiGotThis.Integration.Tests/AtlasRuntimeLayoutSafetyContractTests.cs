namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasRuntimeLayoutSafetyContractTests
{
    [Fact]
    public void Inspector_positioning_is_render_only_and_user_owned_after_initial_placement()
    {
        var root = FindRepositoryRoot();
        var presentation = Path.Combine(root, "src", "WiiiiGotThis.Presentation");
        var baseSource = File.ReadAllText(Path.Combine(presentation, "DesktopAtlasView.axaml.cs"));
        var polishSource = File.ReadAllText(Path.Combine(presentation, "DesktopAtlasView.Polish.cs"));
        var atlasSources = Directory.GetFiles(presentation, "DesktopAtlasView*.cs")
            .Select(File.ReadAllText)
            .ToArray();

        Assert.Contains(
            "private void PositionInspector() => QueueInspectorPlacementRefinement();",
            baseSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "private readonly TranslateTransform inspectorTranslate = new();",
            polishSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "InspectorCard.RenderTransform = inspectorTranslate;",
            polishSource,
            StringComparison.Ordinal);
        Assert.Contains("private bool inspectorHasPlacement;", polishSource, StringComparison.Ordinal);
        Assert.Contains("private bool inspectorDragging;", polishSource, StringComparison.Ordinal);
        Assert.Contains("inspectorTranslate.X = Math.Clamp", polishSource, StringComparison.Ordinal);
        Assert.Contains("inspectorTranslate.Y = Math.Clamp", polishSource, StringComparison.Ordinal);

        var cameraHandlerStart = polishSource.IndexOf("private void OnAtlasCameraTransformChanged", StringComparison.Ordinal);
        var nextHandler = polishSource.IndexOf("private void OnInspectorSizeChanged", cameraHandlerStart, StringComparison.Ordinal);
        Assert.True(cameraHandlerStart >= 0 && nextHandler > cameraHandlerStart);
        var cameraHandler = polishSource[cameraHandlerStart..nextHandler];
        Assert.Contains("UpdateProductionSceneCamera();", cameraHandler, StringComparison.Ordinal);
        Assert.Contains("UpdateInspectorTether();", cameraHandler, StringComparison.Ordinal);
        Assert.DoesNotContain("QueueInspectorPlacementRefinement();", cameraHandler, StringComparison.Ordinal);

        Assert.All(atlasSources, source =>
        {
            Assert.DoesNotContain("InspectorCard.Margin =", source, StringComparison.Ordinal);
            Assert.DoesNotContain("LayoutUpdated", source, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Inspector_size_changes_can_clamp_render_position_without_mutating_layout_geometry()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.Polish.cs"));

        Assert.Contains("InspectorCard.SizeChanged += OnInspectorSizeChanged;", source, StringComparison.Ordinal);
        Assert.Contains("QueueInspectorPlacementRefinement();", source, StringComparison.Ordinal);
        Assert.Contains("DispatcherPriority.Render", source, StringComparison.Ordinal);
        Assert.Contains("var left = inspectorTranslate.X;", source, StringComparison.Ordinal);
        Assert.Contains("var top = inspectorTranslate.Y;", source, StringComparison.Ordinal);
        Assert.Contains("OnInspectorHeaderPointerPressed", source, StringComparison.Ordinal);
        Assert.Contains("OnInspectorDragMoved", source, StringComparison.Ordinal);
        Assert.Contains("OnInspectorDragReleased", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InspectorCard.Margin", source, StringComparison.Ordinal);
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
