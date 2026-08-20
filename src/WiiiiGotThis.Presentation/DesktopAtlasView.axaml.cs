using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopAtlasView : UserControl
{
    private const double WorldWidth = 2000;
    private const double WorldHeight = 1320;
    private const double WorldCenterX = WorldWidth / 2;
    private const double WorldCenterY = WorldHeight / 2;
    private const double InitialZoom = 0.82;
    private const double MinimumZoom = 0.55;
    private const double MaximumZoom = 1.8;
    private const double GridSpacing = 100;
    private const double DragThreshold = 4;

    private static readonly string[] ThemeClasses =
    [
        "theme-technical",
        "theme-elegant",
        "theme-machine",
        "theme-world"
    ];

    private readonly ScaleTransform sceneScale = new() { ScaleX = InitialZoom, ScaleY = InitialZoom };
    private readonly TranslateTransform sceneTranslate = new();
    private ShellViewModel? shell;
    private AtlasThemePreference visualTheme = AtlasThemePreference.Technical;
    private bool isDragging;
    private bool dragMoved;
    private bool cameraInitialized;
    private Point dragStart;
    private Vector translateStart;

    public DesktopAtlasView()
    {
        InitializeComponent();
        var transform = new TransformGroup();
        transform.Children.Add(sceneScale);
        transform.Children.Add(sceneTranslate);
        SceneCanvas.RenderTransform = transform;
        ApplyVisualTheme(AtlasThemePreference.Technical);

        DataContextChanged += OnDataContextChanged;
        AttachedToVisualTree += (_, _) => AttachShell(DataContext as ShellViewModel);
        DetachedFromVisualTree += (_, _) => AttachShell(null);
    }

    public bool FocusPrimaryControl() => AtlasSearch.Focus();

    private void OnDataContextChanged(object? sender, EventArgs e) => AttachShell(DataContext as ShellViewModel);

    private void AttachShell(ShellViewModel? next)
    {
        if (ReferenceEquals(shell, next))
            return;

        if (shell is not null)
        {
            shell.PropertyChanged -= OnShellPropertyChanged;
            shell.AtlasNodes.CollectionChanged -= OnAtlasCollectionChanged;
            shell.AtlasConnections.CollectionChanged -= OnAtlasCollectionChanged;
        }

        shell = next;
        if (shell is not null)
        {
            shell.PropertyChanged += OnShellPropertyChanged;
            shell.AtlasNodes.CollectionChanged += OnAtlasCollectionChanged;
            shell.AtlasConnections.CollectionChanged += OnAtlasCollectionChanged;
            ApplyVisualTheme(shell.AtlasTheme);
        }

        RebuildScene();
    }

    private void OnAtlasCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildScene();

    private void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShellViewModel.SelectedAtlasNode))
        {
            RefreshNodeSelection();
            PositionInspector();
        }
        else if (e.PropertyName == nameof(ShellViewModel.AtlasSettingsExpanded) && shell?.AtlasSettingsExpanded != true)
        {
            ThemeChoices.IsVisible = false;
        }
        else if (e.PropertyName == nameof(ShellViewModel.AtlasTheme) && shell is not null)
        {
            ApplyVisualTheme(shell.AtlasTheme);
        }
    }

    private void RebuildScene()
    {
        SceneCanvas.Children.Clear();
        var currentShell = shell;
        if (currentShell is null)
            return;

        if (IsProductionSceneRendererActive)
        {
            AttachProductionRendererShell(currentShell);
            UpdateProductionScene();
            PositionInspector();
            return;
        }

        AddGridLines();

        foreach (var connection in currentShell.AtlasConnections)
            SceneCanvas.Children.Add(CreateConnectionPath(connection));

        foreach (var node in currentShell.AtlasNodes)
            SceneCanvas.Children.Add(CreateNodeVisual(node, currentShell));

        RefreshNodeSelection();
        PositionInspector();
    }

    private void AddGridLines()
    {
        for (var x = 0d; x <= WorldWidth; x += GridSpacing)
        {
            var line = new Line
            {
                StartPoint = new Point(x, 0),
                EndPoint = new Point(x, WorldHeight),
                IsHitTestVisible = false
            };
            line.Classes.Add("wgt-atlas-gridline");
            ApplyThemeClass(line);
            SceneCanvas.Children.Add(line);
        }

        for (var y = 0d; y <= WorldHeight; y += GridSpacing)
        {
            var line = new Line
            {
                StartPoint = new Point(0, y),
                EndPoint = new Point(WorldWidth, y),
                IsHitTestVisible = false
            };
            line.Classes.Add("wgt-atlas-gridline");
            ApplyThemeClass(line);
            SceneCanvas.Children.Add(line);
        }
    }

    private Avalonia.Controls.Shapes.Path CreateConnectionPath(AtlasConnectionPresentationViewModel connection)
    {
        var start = WorldPoint(connection.Source);
        var end = WorldPoint(connection.Target);
        var deltaX = end.X - start.X;
        var deltaY = end.Y - start.Y;
        var length = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        var bend = connection.Kind switch
        {
            AtlasConnectionKind.CapabilityDependency => 58d,
            AtlasConnectionKind.CapabilityOwnership => 28d,
            _ => 20d
        };
        var direction = StableCurveDirection(connection.Model.ConnectionId);
        var control = length < 0.001
            ? new Point((start.X + end.X) / 2, (start.Y + end.Y) / 2)
            : new Point(
                (start.X + end.X) / 2 + (-deltaY / length) * bend * direction,
                (start.Y + end.Y) / 2 + (deltaX / length) * bend * direction);

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(start, isFilled: false);
            context.QuadraticBezierTo(control, end, isStroked: true);
            context.EndFigure(isClosed: false);
        }

        var path = new Avalonia.Controls.Shapes.Path
        {
            Data = geometry,
            DataContext = connection,
            IsHitTestVisible = false
        };
        path.Classes.Add("wgt-atlas-connection");
        if (connection.Kind == AtlasConnectionKind.CapabilityOwnership)
            path.Classes.Add("capability");
        else if (connection.Kind == AtlasConnectionKind.CapabilityDependency)
            path.Classes.Add("dependency");
        ApplyThemeClass(path);
        return path;
    }

    private Border CreateNodeVisual(AtlasNodePresentationViewModel node, ShellViewModel currentShell)
    {
        var (width, height) = node.Kind switch
        {
            AtlasNodeKind.Core => (184d, 184d),
            AtlasNodeKind.Service => (146d, 146d),
            _ => (154d, 60d)
        };

        var kind = new TextBlock
        {
            Text = node.Kind switch
            {
                AtlasNodeKind.Core => "WGT CORE",
                AtlasNodeKind.Service => "SERVICE",
                _ => "CAPABILITY"
            },
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        kind.Classes.Add("wgt-node-kind");

        var title = new TextBlock
        {
            Text = node.Title,
            FontWeight = FontWeight.SemiBold,
            FontSize = node.IsCore ? 19 : node.IsService ? 15 : 12,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = width - 26
        };

        var statusDot = new Border
        {
            Width = 7,
            Height = 7,
            CornerRadius = new CornerRadius(4),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        statusDot.Classes.Add("wgt-atlas-status-dot");
        if (!node.IsAvailable)
            statusDot.Classes.Add("unavailable");

        var state = new TextBlock
        {
            Text = node.CompactStateText,
            FontSize = 10,
            FontWeight = FontWeight.SemiBold,
            Opacity = 0.72,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = width - 38
        };

        var stateRow = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 6,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Children = { statusDot, state }
        };

        var content = new StackPanel
        {
            Spacing = node.IsCapability ? 2 : 6,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Children = { kind, title, stateRow }
        };

        var button = new Button
        {
            Width = width,
            Height = height,
            Padding = new Thickness(node.IsCapability ? 8 : 12),
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Content = content,
            DataContext = node,
            Command = currentShell.SelectAtlasNodeCommand,
            CommandParameter = node
        };
        button.Classes.Add("wgt-atlas-node");
        button.Classes.Add(NodeKindClass(node.Kind));
        if (!node.IsAvailable)
            button.Classes.Add("unavailable");
        ApplyThemeClass(button);

        var nodeShell = new Border
        {
            Width = width,
            Height = height,
            DataContext = node,
            Child = button
        };
        nodeShell.Classes.Add("wgt-atlas-node-shell");
        nodeShell.Classes.Add(NodeKindClass(node.Kind));
        if (!node.IsAvailable)
            nodeShell.Classes.Add("unavailable");
        ApplyThemeClass(nodeShell);

        var world = WorldPoint(node);
        Canvas.SetLeft(nodeShell, world.X - width / 2);
        Canvas.SetTop(nodeShell, world.Y - height / 2);
        return nodeShell;
    }

    private void RefreshNodeSelection()
    {
        var selectedId = shell?.SelectedAtlasNode?.NodeId;
        var focusNodeIds = BuildFocusNodeSet(selectedId);

        foreach (var nodeShell in SceneCanvas.Children.OfType<Border>())
        {
            if (!nodeShell.Classes.Contains("wgt-atlas-node-shell") || nodeShell.DataContext is not AtlasNodePresentationViewModel node)
                continue;

            var selected = string.Equals(node.NodeId, selectedId, StringComparison.Ordinal);
            var contextual = selectedId is not null && !selected && focusNodeIds.Contains(node.NodeId);
            var dimmed = selectedId is not null && !focusNodeIds.Contains(node.NodeId);
            SetSelectedClass(nodeShell, selected);
            SetStateClass(nodeShell, "contextual", contextual);
            SetStateClass(nodeShell, "dimmed", dimmed);
            if (nodeShell.Child is Button button)
            {
                SetSelectedClass(button, selected);
                SetStateClass(button, "contextual", contextual);
                SetStateClass(button, "dimmed", dimmed);
            }
        }

        foreach (var path in SceneCanvas.Children.OfType<Avalonia.Controls.Shapes.Path>())
        {
            if (!path.Classes.Contains("wgt-atlas-connection") || path.DataContext is not AtlasConnectionPresentationViewModel connection)
                continue;

            var focused = selectedId is not null
                && focusNodeIds.Contains(connection.Source.NodeId)
                && focusNodeIds.Contains(connection.Target.NodeId);
            SetStateClass(path, "focused", focused);
            SetStateClass(path, "dimmed", selectedId is not null && !focused);
        }
    }

    private IReadOnlySet<string> BuildFocusNodeSet(string? selectedId) =>
        shell is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : AtlasPresentationFocus.Build(shell.AtlasConnections, selectedId);

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (!cameraInitialized && e.NewSize.Width > 0 && e.NewSize.Height > 0)
        {
            ResetCamera();
            cameraInitialized = true;
        }
        PositionInspector();
    }

    private void ResetCamera()
    {
        sceneScale.ScaleX = InitialZoom;
        sceneScale.ScaleY = InitialZoom;
        sceneTranslate.X = AtlasViewport.Bounds.Width / 2 - WorldCenterX * InitialZoom;
        sceneTranslate.Y = AtlasViewport.Bounds.Height / 2 - WorldCenterY * InitialZoom;
        PositionInspector();
    }

    private void CenterOnSelected()
    {
        if (shell?.SelectedAtlasNode is not { } node)
            return;
        var world = WorldPoint(node);
        sceneTranslate.X = AtlasViewport.Bounds.Width / 2 - world.X * sceneScale.ScaleX;
        sceneTranslate.Y = AtlasViewport.Bounds.Height / 2 - world.Y * sceneScale.ScaleY;
        PositionInspector();
    }

    private void PositionInspector() => QueueInspectorPlacementRefinement();

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(AtlasViewport);
        if (!point.Properties.IsLeftButtonPressed || IsInteractiveSource(e.Source))
            return;

        isDragging = true;
        dragMoved = false;
        dragStart = e.GetPosition(AtlasViewport);
        translateStart = new Vector(sceneTranslate.X, sceneTranslate.Y);
        e.Pointer.Capture(AtlasViewport);
        AtlasViewport.Focus();
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!isDragging)
            return;
        var current = e.GetPosition(AtlasViewport);
        var deltaX = current.X - dragStart.X;
        var deltaY = current.Y - dragStart.Y;
        if (!dragMoved && Math.Sqrt(deltaX * deltaX + deltaY * deltaY) >= DragThreshold)
            dragMoved = true;
        sceneTranslate.X = translateStart.X + deltaX;
        sceneTranslate.Y = translateStart.Y + deltaY;
        PositionInspector();
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!isDragging)
            return;

        var closeContext = !dragMoved;
        isDragging = false;
        e.Pointer.Capture(null);
        if (closeContext)
            CloseTransientAtlasContext();
        e.Handled = true;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (IsInteractiveSource(e.Source))
            return;
        var factor = e.Delta.Y > 0 ? 1.1 : 0.9;
        ZoomAt(e.GetPosition(AtlasViewport), factor);
        e.Handled = true;
    }

    private void ZoomAt(Point screenPoint, double factor)
    {
        var oldZoom = sceneScale.ScaleX;
        var newZoom = Math.Clamp(oldZoom * factor, MinimumZoom, MaximumZoom);
        if (Math.Abs(newZoom - oldZoom) < 0.0001)
            return;

        var worldX = (screenPoint.X - sceneTranslate.X) / oldZoom;
        var worldY = (screenPoint.Y - sceneTranslate.Y) / oldZoom;
        sceneScale.ScaleX = newZoom;
        sceneScale.ScaleY = newZoom;
        sceneTranslate.X = screenPoint.X - worldX * newZoom;
        sceneTranslate.Y = screenPoint.Y - worldY * newZoom;
        PositionInspector();
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || shell is null)
            return;
        shell.SearchAtlasCommand.Execute(null);
        CenterOnSelected();
        e.Handled = true;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        const double step = 56;
        switch (e.Key)
        {
            case Key.Left:
                sceneTranslate.X += step;
                break;
            case Key.Right:
                sceneTranslate.X -= step;
                break;
            case Key.Up:
                sceneTranslate.Y += step;
                break;
            case Key.Down:
                sceneTranslate.Y -= step;
                break;
            case Key.PageUp:
                ZoomAt(new Point(AtlasViewport.Bounds.Width / 2, AtlasViewport.Bounds.Height / 2), 1.1);
                break;
            case Key.PageDown:
                ZoomAt(new Point(AtlasViewport.Bounds.Width / 2, AtlasViewport.Bounds.Height / 2), 0.9);
                break;
            case Key.R:
                ResetCamera();
                break;
            case Key.Escape:
                CloseTransientAtlasContext();
                break;
            default:
                return;
        }
        PositionInspector();
        e.Handled = true;
    }

    private void OnCloseInspector(object? sender, RoutedEventArgs e)
    {
        shell?.SelectAtlasNodeCommand.Execute(null);
        AtlasViewport.Focus();
        e.Handled = true;
    }

    private void CloseTransientAtlasContext()
    {
        shell?.SelectAtlasNodeCommand.Execute(null);
        ThemeChoices.IsVisible = false;
        if (shell?.AtlasSettingsExpanded == true)
            shell.ToggleAtlasSettingsCommand.Execute(null);
    }

    private void OnToggleThemeMenu(object? sender, RoutedEventArgs e)
    {
        ThemeChoices.IsVisible = !ThemeChoices.IsVisible;
        e.Handled = true;
    }

    private async void OnTechnicalTheme(object? sender, RoutedEventArgs e) => await SelectVisualThemeAsync(AtlasThemePreference.Technical, e);
    private async void OnElegantTheme(object? sender, RoutedEventArgs e) => await SelectVisualThemeAsync(AtlasThemePreference.Elegant, e);
    private async void OnMachineTheme(object? sender, RoutedEventArgs e) => await SelectVisualThemeAsync(AtlasThemePreference.Machine, e);
    private async void OnWorldTheme(object? sender, RoutedEventArgs e) => await SelectVisualThemeAsync(AtlasThemePreference.World, e);

    private async Task SelectVisualThemeAsync(AtlasThemePreference theme, RoutedEventArgs e)
    {
        if (shell is not null)
            await shell.SetAtlasThemeAsync(theme);
        else
            ApplyVisualTheme(theme);

        ThemeChoices.IsVisible = false;
        e.Handled = true;
    }

    private void ApplyVisualTheme(AtlasThemePreference theme)
    {
        visualTheme = theme;
        ApplyThemeClass(AtlasViewport);
        ApplyThemeClass(InspectorCard);
        ApplyThemeClass(ControlHint);
        ApplyThemeClass(ThemeMenuButton);

        foreach (var element in SceneCanvas.Children.OfType<StyledElement>())
        {
            ApplyThemeClass(element);
            if (element is Border { Child: StyledElement child } && element.Classes.Contains("wgt-atlas-node-shell"))
                ApplyThemeClass(child);
        }

        SetSelectedClass(TechnicalThemeButton, theme == AtlasThemePreference.Technical);
        SetSelectedClass(ElegantThemeButton, theme == AtlasThemePreference.Elegant);
        SetSelectedClass(MachineThemeButton, theme == AtlasThemePreference.Machine);
        SetSelectedClass(WorldThemeButton, theme == AtlasThemePreference.World);
    }

    private void ApplyThemeClass(StyledElement element)
    {
        foreach (var themeClass in ThemeClasses)
            element.Classes.Remove(themeClass);
        element.Classes.Add(visualTheme switch
        {
            AtlasThemePreference.Technical => "theme-technical",
            AtlasThemePreference.Elegant => "theme-elegant",
            AtlasThemePreference.Machine => "theme-machine",
            AtlasThemePreference.World => "theme-world",
            _ => "theme-technical"
        });
    }

    private static void SetSelectedClass(StyledElement element, bool selected) =>
        SetStateClass(element, "selected", selected);

    private static void SetStateClass(StyledElement element, string className, bool enabled)
    {
        if (enabled)
        {
            if (!element.Classes.Contains(className))
                element.Classes.Add(className);
        }
        else
        {
            element.Classes.Remove(className);
        }
    }

    private bool IsInteractiveSource(object? source)
    {
        if (source is not Visual visual)
            return false;

        if (ReferenceEquals(visual, InspectorCard) || visual.GetVisualAncestors().Any(ancestor => ReferenceEquals(ancestor, InspectorCard)))
            return true;

        return visual is Button or TextBox or TabControl or ListBox ||
               visual.FindAncestorOfType<Button>() is not null ||
               visual.FindAncestorOfType<TextBox>() is not null ||
               visual.FindAncestorOfType<TabControl>() is not null ||
               visual.FindAncestorOfType<ListBox>() is not null;
    }

    private static double StableCurveDirection(string connectionId)
    {
        var checksum = 0;
        foreach (var character in connectionId)
            checksum = (checksum + character) % 2;
        return checksum == 0 ? 1 : -1;
    }

    private static string NodeKindClass(AtlasNodeKind kind) => kind switch
    {
        AtlasNodeKind.Core => "core",
        AtlasNodeKind.Service => "service",
        _ => "capability"
    };

    private static Point WorldPoint(AtlasNodePresentationViewModel node) =>
        new(WorldCenterX + node.X, WorldCenterY + node.Y);
}