using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopAtlasView : UserControl
{
    private const double WorldWidth = 1800;
    private const double WorldHeight = 1200;
    private const double WorldCenterX = WorldWidth / 2;
    private const double WorldCenterY = WorldHeight / 2;
    private const double MinimumZoom = 0.55;
    private const double MaximumZoom = 1.8;

    private readonly ScaleTransform sceneScale = new() { ScaleX = 1, ScaleY = 1 };
    private readonly TranslateTransform sceneTranslate = new();
    private ShellViewModel? shell;
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
    }

    private void RebuildScene()
    {
        SceneCanvas.Children.Clear();
        var currentShell = shell;
        if (currentShell is null)
            return;

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
            SceneCanvas.Children.Add(line);
        }

        foreach (var node in currentShell.AtlasNodes)
            SceneCanvas.Children.Add(CreateNodeButton(node, currentShell));

        RefreshNodeSelection();
        PositionInspector();
    }

    private static Button CreateNodeButton(AtlasNodePresentationViewModel node, ShellViewModel currentShell)
    {
        var (width, height) = node.Kind switch
        {
            AtlasNodeKind.Core => (184d, 92d),
            AtlasNodeKind.Service => (158d, 78d),
            _ => (136d, 62d)
        };

        var title = new TextBlock
        {
            Text = node.Title,
            FontWeight = FontWeight.SemiBold,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = width - 24
        };
        var state = new TextBlock
        {
            Text = node.IsCore ? "CORE" : node.AvailabilityText,
            FontSize = 11,
            Opacity = 0.72,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = width - 24
        };
        var content = new StackPanel
        {
            Spacing = 3,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Children = { title, state }
        };
        var button = new Button
        {
            Width = width,
            Height = height,
            Padding = new Thickness(10, 8),
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
        const double cardWidth = 336;
        const double estimatedHeight = 430;
        const double gap = 28;

        var left = x + gap;
        if (left + cardWidth > AtlasViewport.Bounds.Width - 18)
            left = x - cardWidth - gap;
        left = Math.Clamp(left, 18, Math.Max(18, AtlasViewport.Bounds.Width - cardWidth - 18));

        var top = Math.Clamp(y - 96, 70, Math.Max(70, AtlasViewport.Bounds.Height - estimatedHeight - 18));
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
