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
    private const double MinimumZoom = 0.55;
    private const double MaximumZoom = 1.8;
    private const double GridSpacing = 100;

    private static readonly string[] ThemeClasses =
    [
        "theme-technical",
        "theme-elegant",
        "theme-machine",
        "theme-world"
    ];

    private readonly ScaleTransform sceneScale = new() { ScaleX = 1, ScaleY = 1 };
    private readonly TranslateTransform sceneTranslate = new();
    private ShellViewModel? shell;
    private AtlasThemePreference visualTheme = AtlasThemePreference.Technical;
    private bool isDragging;
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

        AddGridLines();

        foreach (var connection in currentShell.AtlasConnections)
        {
            var line = new Line
            {
                StartPoint = WorldPoint(connection.Source),
                EndPoint = WorldPoint(connection.Target),
                IsHitTestVisible = false
            };
            line.Classes.Add("wgt-atlas-connection");
            if (connection.Kind == AtlasConnectionKind.CapabilityOwnership)
                line.Classes.Add("capability");
            else if (connection.Kind == AtlasConnectionKind.CapabilityDependency)
                line.Classes.Add("dependency");
            ApplyThemeClass(line);
            SceneCanvas.Children.Add(line);
        }

        foreach (var node in currentShell.AtlasNodes)
        {
            var button = CreateNodeButton(node, currentShell);
            ApplyThemeClass(button);
            SceneCanvas.Children.Add(button);
        }

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

    private static Button CreateNodeButton(AtlasNodePresentationViewModel node, ShellViewModel currentShell)
    {
        var (width, height) = node.Kind switch
        {
            AtlasNodeKind.Core => (210d, 108d),
            AtlasNodeKind.Service => (178d, 92d),
            _ => (146d, 68d)
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
            FontSize = node.IsCore ? 17 : 15,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = width - 24
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
            Text = node.IsCore ? "system space" : node.AvailabilityText,
            FontSize = 11,
            Opacity = 0.76,
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
            Spacing = node.IsCapability ? 3 : 5,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Children = { kind, title, stateRow }
        };

        var button = new Button
        {
            Width = width,
            Height = height,
            Padding = new Thickness(11, 8),
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Content = content,
            DataContext = node,
            Command = currentShell.SelectAtlasNodeCommand,
            CommandParameter = node
        };
        button.Classes.Add("wgt-atlas-node");
        button.Classes.Add(node.Kind switch
        {
            AtlasNodeKind.Core => "core",
            AtlasNodeKind.Service => "service",
            _ => "capability"
        });
        if (!node.IsAvailable)
            button.Classes.Add("unavailable");

        var world = WorldPoint(node);
        Canvas.SetLeft(button, world.X - width / 2);
        Canvas.SetTop(button, world.Y - height / 2);
        return button;
    }

    private void RefreshNodeSelection()
    {
        var selectedId = shell?.SelectedAtlasNode?.NodeId;
        foreach (var button in SceneCanvas.Children.OfType<Button>())
        {
            if (button.DataContext is not AtlasNodePresentationViewModel node)
                continue;
            if (string.Equals(node.NodeId, selectedId, StringComparison.Ordinal))
            {
                if (!button.Classes.Contains("selected"))
                    button.Classes.Add("selected");
            }
            else
            {
                button.Classes.Remove("selected");
            }
        }
    }

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
        sceneScale.ScaleX = 1;
        sceneScale.ScaleY = 1;
        sceneTranslate.X = AtlasViewport.Bounds.Width / 2 - WorldCenterX;
        sceneTranslate.Y = AtlasViewport.Bounds.Height / 2 - WorldCenterY;
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

    private void PositionInspector()
    {
        if (shell?.SelectedAtlasNode is not { } node || AtlasViewport.Bounds.Width <= 0)
            return;

        var world = WorldPoint(node);
        var x = world.X * sceneScale.ScaleX + sceneTranslate.X;
        var y = world.Y * sceneScale.ScaleY + sceneTranslate.Y;
        const double cardWidth = 382;
        const double estimatedHeight = 500;
        const double gap = 30;

        var left = x + gap;
        if (left + cardWidth > AtlasViewport.Bounds.Width - 20)
            left = x - cardWidth - gap;
        left = Math.Clamp(left, 20, Math.Max(20, AtlasViewport.Bounds.Width - cardWidth - 20));

        var top = Math.Clamp(y - 110, 88, Math.Max(88, AtlasViewport.Bounds.Height - estimatedHeight - 20));
        Canvas.SetLeft(InspectorCard, left);
        Canvas.SetTop(InspectorCard, top);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(AtlasViewport);
        if (!point.Properties.IsLeftButtonPressed || IsInteractiveSource(e.Source))
            return;

        isDragging = true;
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
        sceneTranslate.X = translateStart.X + current.X - dragStart.X;
        sceneTranslate.Y = translateStart.Y + current.Y - dragStart.Y;
        PositionInspector();
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!isDragging)
            return;
        isDragging = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
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
                shell?.SelectAtlasNodeCommand.Execute(null);
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
            ApplyThemeClass(element);

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

    private static void SetSelectedClass(StyledElement element, bool selected)
    {
        if (selected)
        {
            if (!element.Classes.Contains("selected"))
                element.Classes.Add("selected");
        }
        else
        {
            element.Classes.Remove("selected");
        }
    }

    private static bool IsInteractiveSource(object? source)
    {
        if (source is not Visual visual)
            return false;
        return visual is Button or TextBox or TabControl or ListBox ||
               visual.FindAncestorOfType<Button>() is not null ||
               visual.FindAncestorOfType<TextBox>() is not null ||
               visual.FindAncestorOfType<TabControl>() is not null ||
               visual.FindAncestorOfType<ListBox>() is not null;
    }

    private static Point WorldPoint(AtlasNodePresentationViewModel node) =>
        new(WorldCenterX + node.X, WorldCenterY + node.Y);
}
