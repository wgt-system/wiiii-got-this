using Avalonia;
using Avalonia.Threading;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopAtlasView
{
    private bool initialOverviewFitQueued;
    private bool initialOverviewFitApplied;

    private void QueueInitialOverviewFit()
    {
        if (initialOverviewFitQueued || initialOverviewFitApplied)
            return;

        initialOverviewFitQueued = true;
        Dispatcher.UIThread.Post(
            () =>
            {
                initialOverviewFitQueued = false;
                if (initialOverviewFitApplied || AtlasViewport.Bounds.Width <= 0 || AtlasViewport.Bounds.Height <= 0)
                    return;

                if (FitOverviewCamera())
                    initialOverviewFitApplied = true;
            },
            DispatcherPriority.Render);
    }

    private bool FitOverviewCamera()
    {
        var currentShell = shell ?? experienceShell;
        if (currentShell is null || AtlasViewport.Bounds.Width <= 0 || AtlasViewport.Bounds.Height <= 0)
            return false;

        var primaryNodes = currentShell.AtlasNodes
            .Where(node => node.IsCore || node.IsService)
            .ToArray();
        if (primaryNodes.Length == 0)
            return false;

        const double serviceHalfWidth = 86d;
        const double serviceHalfHeight = 92d;
        const double coreHalfWidth = 108d;
        const double coreHalfHeight = 108d;

        var minX = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var minY = double.PositiveInfinity;
        var maxY = double.NegativeInfinity;
        foreach (var node in primaryNodes)
        {
            var point = WorldPoint(node);
            var halfWidth = node.IsCore ? coreHalfWidth : serviceHalfWidth;
            var halfHeight = node.IsCore ? coreHalfHeight : serviceHalfHeight;
            minX = Math.Min(minX, point.X - halfWidth);
            maxX = Math.Max(maxX, point.X + halfWidth);
            minY = Math.Min(minY, point.Y - halfHeight);
            maxY = Math.Max(maxY, point.Y + halfHeight);
        }

        const double horizontalSafeArea = 72d;
        const double topSafeArea = 104d;
        const double bottomSafeArea = 42d;
        var availableWidth = Math.Max(320d, AtlasViewport.Bounds.Width - horizontalSafeArea * 2);
        var availableHeight = Math.Max(280d, AtlasViewport.Bounds.Height - topSafeArea - bottomSafeArea);
        var contentWidth = Math.Max(1d, maxX - minX);
        var contentHeight = Math.Max(1d, maxY - minY);
        var fittedZoom = Math.Clamp(
            Math.Min(availableWidth / contentWidth, availableHeight / contentHeight),
            0.66d,
            1.08d);

        var worldCenterX = (minX + maxX) / 2d;
        var worldCenterY = (minY + maxY) / 2d;
        var targetScreenX = AtlasViewport.Bounds.Width / 2d;
        var targetScreenY = topSafeArea + availableHeight / 2d;

        sceneScale.ScaleX = fittedZoom;
        sceneScale.ScaleY = fittedZoom;
        sceneTranslate.X = targetScreenX - worldCenterX * fittedZoom;
        sceneTranslate.Y = targetScreenY - worldCenterY * fittedZoom;
        QueueInspectorPlacementRefinement();
        UpdateInspectorTether();
        return true;
    }
}
