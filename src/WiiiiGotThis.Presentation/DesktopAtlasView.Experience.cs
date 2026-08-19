using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopAtlasView
{
    private Canvas? spatialDepthLayer;
    private ShellViewModel? experienceShell;
    private TextBlock? themeChooserHeaderText;
    private bool themeChooserPrepared;
    private bool spatialDepthRebuildQueued;

    private void AttachExperienceShell(ShellViewModel? next)
    {
        if (ReferenceEquals(experienceShell, next))
            return;

        if (experienceShell is not null)
        {
            experienceShell.AtlasNodes.CollectionChanged -= OnExperienceAtlasNodesChanged;
            experienceShell.PropertyChanged -= OnExperienceShellPropertyChanged;
        }

        experienceShell = next;
        if (experienceShell is not null)
        {
            experienceShell.AtlasNodes.CollectionChanged += OnExperienceAtlasNodesChanged;
            experienceShell.PropertyChanged += OnExperienceShellPropertyChanged;
        }

        EnsureFinalVisualExperience();
        EnsureThemeChooserExperience();
        QueueSpatialDepthRebuild();
        UpdateExperienceState();
    }

    private void OnExperienceAtlasNodesChanged(object? sender, NotifyCollectionChangedEventArgs e) => QueueSpatialDepthRebuild();

    private void OnExperienceShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (experienceShell is null)
            return;

        if (e.PropertyName == nameof(ShellViewModel.AtlasSettingsExpanded))
        {
            if (experienceShell.AtlasSettingsExpanded && !string.IsNullOrWhiteSpace(experienceShell.AtlasSearchText))
                experienceShell.AtlasSearchText = string.Empty;
            UpdateExperienceState();
            return;
        }

        if (e.PropertyName == nameof(ShellViewModel.AtlasTheme))
        {
            UpdateExperienceState();
            if (experienceShell.AtlasSettingsExpanded && experienceShell.ToggleAtlasSettingsCommand.CanExecute(null))
                experienceShell.ToggleAtlasSettingsCommand.Execute(null);
            return;
        }

        if (e.PropertyName == nameof(ShellViewModel.AtlasSearchText) &&
            !string.IsNullOrWhiteSpace(experienceShell.AtlasSearchText) &&
            experienceShell.AtlasSettingsExpanded &&
            experienceShell.ToggleAtlasSettingsCommand.CanExecute(null))
        {
            experienceShell.ToggleAtlasSettingsCommand.Execute(null);
        }
    }

    private void QueueSpatialDepthRebuild()
    {
        if (spatialDepthRebuildQueued || experienceShell is null)
            return;

        spatialDepthRebuildQueued = true;
        Dispatcher.UIThread.Post(
            () =>
            {
                spatialDepthRebuildQueued = false;
                if (experienceShell is null)
                    return;

                EnsureFinalVisualExperience();
                UpgradeFinalNodeVisuals();
                RebuildThemeNodeDecorations();
                RebuildSpatialDepthField();
                WireDirectProductEntry();
            },
            DispatcherPriority.Render);
    }

    private void WireDirectProductEntry()
    {
        foreach (var nodeShell in SceneCanvas.Children.OfType<Border>())
        {
            if (!nodeShell.Classes.Contains("wgt-atlas-node-shell") ||
                nodeShell.Child is not Button button ||
                button.DataContext is not AtlasNodePresentationViewModel node ||
                !node.CanOpenProductSurface ||
                button.Classes.Contains("direct-product-entry"))
            {
                continue;
            }

            button.Classes.Add("direct-product-entry");
            button.DoubleTapped += OnProductNodeDoubleTapped;
            button.KeyDown += OnProductNodeKeyDown;
            ToolTip.SetTip(button, $"Select {node.Title} · double-click or press Enter to open");
        }
    }

    private async void OnProductNodeDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Button { DataContext: AtlasNodePresentationViewModel node } || shell is null)
            return;

        shell.SelectAtlasNodeCommand.Execute(node);
        if (await OpenSelectedProductSurfaceAsync())
            e.Handled = true;
    }

    private async void OnProductNodeKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not Button { DataContext: AtlasNodePresentationViewModel node } || shell is null)
            return;

        shell.SelectAtlasNodeCommand.Execute(node);
        if (await OpenSelectedProductSurfaceAsync())
            e.Handled = true;
    }

    private void EnsureThemeChooserExperience()
    {
        if (themeChooserPrepared)
            return;

        themeChooserPrepared = true;
        ThemeMenuButton.IsVisible = false;

        if (ThemeMenuButton.Parent is Canvas settingsCanvas)
        {
            settingsCanvas.Width = 232;
            settingsCanvas.Height = 286;
        }

        ThemeChoices.Width = 216;
        ThemeChoices.Orientation = Avalonia.Layout.Orientation.Vertical;
        ThemeChoices.Spacing = 3;
        Canvas.SetRight(ThemeChoices, 0);
        Canvas.SetTop(ThemeChoices, 54);

        themeChooserHeaderText = new TextBlock
        {
            Text = "APPEARANCE",
            FontSize = 9,
            FontWeight = FontWeight.SemiBold,
            LetterSpacing = 1.4,
            Opacity = 0.58,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        var header = new Border
        {
            Height = 28,
            Padding = new Thickness(10, 0),
            Child = themeChooserHeaderText
        };
        header.Classes.Add("wgt-theme-chooser-header");
        ThemeChoices.Children.Insert(0, header);

        ConfigureThemeChoice(TechnicalThemeButton, "Technical", "instrument panel");
        ConfigureThemeChoice(ElegantThemeButton, "Elegant", "quiet material");
        ConfigureThemeChoice(MachineThemeButton, "Machine", "engineered grid");
        ConfigureThemeChoice(WorldThemeButton, "World", "spatial terrain");
    }

    private static void ConfigureThemeChoice(Button button, string title, string description)
    {
        button.Width = 216;
        button.Height = 44;
        button.CornerRadius = new CornerRadius(10);
        button.Padding = new Thickness(8, 5);
        button.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        button.Classes.Add($"preview-{title.ToLowerInvariant()}");
        ToolTip.SetTip(button, $"{title} · {description}");

        var previewFrame = new Border
        {
            Width = 30,
            Height = 30,
            CornerRadius = new CornerRadius(8),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Child = BuildThemePreview(title)
        };
        previewFrame.Classes.Add("wgt-theme-choice-glyph");
        previewFrame.Classes.Add($"preview-{title.ToLowerInvariant()}");

        var text = new StackPanel
        {
            Spacing = 0,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = title,
                    FontSize = 11,
                    FontWeight = FontWeight.SemiBold
                },
                new TextBlock
                {
                    Text = description,
                    FontSize = 8,
                    Opacity = 0.5,
                    TextTrimming = TextTrimming.CharacterEllipsis
                }
            }
        };

        var content = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            ColumnSpacing = 9,
            Children = { previewFrame, text }
        };
        Grid.SetColumn(text, 1);
        button.Content = content;
    }

    private static Canvas BuildThemePreview(string title)
    {
        var preview = new Canvas
        {
            Width = 22,
            Height = 22,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        preview.Classes.Add("wgt-theme-mini-preview");
        preview.Classes.Add($"preview-{title.ToLowerInvariant()}");

        switch (title)
        {
            case "Technical":
                preview.Children.Add(PreviewRail(18, 1, 2, 10, "technical"));
                preview.Children.Add(PreviewRail(1, 18, 10, 2, "technical"));
                preview.Children.Add(PreviewDot(5, 8.5, 8.5, "technical"));
                break;
            case "Elegant":
                preview.Children.Add(PreviewRing(18, 18, 2, 2, "elegant"));
                preview.Children.Add(PreviewRing(8, 8, 7, 7, "elegant"));
                break;
            case "Machine":
                preview.Children.Add(PreviewRail(10, 2, 2, 3, "machine"));
                preview.Children.Add(PreviewRail(2, 10, 2, 3, "machine"));
                preview.Children.Add(PreviewRail(10, 2, 10, 17, "machine"));
                preview.Children.Add(PreviewRail(2, 10, 18, 9, "machine"));
                break;
            case "World":
                preview.Children.Add(PreviewRing(17, 9, 2.5, 10, "world"));
                preview.Children.Add(PreviewDot(4, 5, 5, "world"));
                preview.Children.Add(PreviewDot(3, 15, 4, "world"));
                preview.Children.Add(PreviewDot(3, 12, 16, "world"));
                break;
        }

        return preview;
    }

    private static Border PreviewRail(double width, double height, double left, double top, string kind)
    {
        var rail = new Border { Width = width, Height = height, IsHitTestVisible = false };
        rail.Classes.Add("wgt-theme-preview-mark");
        rail.Classes.Add(kind);
        Canvas.SetLeft(rail, left);
        Canvas.SetTop(rail, top);
        return rail;
    }

    private static Border PreviewRing(double width, double height, double left, double top, string kind)
    {
        var ring = new Border
        {
            Width = width,
            Height = height,
            CornerRadius = new CornerRadius(Math.Min(width, height) / 2),
            BorderThickness = new Thickness(1),
            IsHitTestVisible = false
        };
        ring.Classes.Add("wgt-theme-preview-ring");
        ring.Classes.Add(kind);
        Canvas.SetLeft(ring, left);
        Canvas.SetTop(ring, top);
        return ring;
    }

    private static Border PreviewDot(double size, double left, double top, string kind)
    {
        var dot = new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(size / 2),
            IsHitTestVisible = false
        };
        dot.Classes.Add("wgt-theme-preview-dot");
        dot.Classes.Add(kind);
        Canvas.SetLeft(dot, left);
        Canvas.SetTop(dot, top);
        return dot;
    }

    private void UpdateExperienceState()
    {
        if (!themeChooserPrepared)
            return;

        var theme = experienceShell?.AtlasTheme ?? visualTheme;
        if (themeChooserHeaderText is not null)
            themeChooserHeaderText.Text = $"APPEARANCE  ·  {theme.ToString().ToUpperInvariant()}";

        ThemeChoices.IsVisible = experienceShell?.AtlasSettingsExpanded == true;
        ToolTip.SetTip(ThemeMenuButton, $"Theme · {theme}");
        UpdateSpatialDepthSelection();
    }

    private void RebuildSpatialDepthField()
    {
        if (spatialDepthLayer is not null)
            SceneCanvas.Children.Remove(spatialDepthLayer);

        var currentShell = experienceShell ?? shell;
        if (currentShell is null || currentShell.AtlasNodes.Count == 0)
        {
            spatialDepthLayer = null;
            return;
        }

        spatialDepthLayer = new Canvas
        {
            Width = WorldWidth,
            Height = WorldHeight,
            IsHitTestVisible = false
        };
        spatialDepthLayer.Classes.Add("wgt-atlas-depth-layer");

        foreach (var node in currentShell.AtlasNodes.Where(node => node.IsCore || node.IsService))
        {
            var diameter = node.IsCore ? 250d : 178d;
            AddDepthRing(node, diameter, "focus-field");
        }

        var insertIndex = 0;
        while (insertIndex < SceneCanvas.Children.Count && SceneCanvas.Children[insertIndex] is Avalonia.Controls.Shapes.Line)
            insertIndex++;
        SceneCanvas.Children.Insert(insertIndex, spatialDepthLayer);
        UpdateSpatialDepthSelection();
    }

    private void AddDepthRing(AtlasNodePresentationViewModel node, double diameter, string layerClass)
    {
        var ring = new Border
        {
            Width = diameter,
            Height = diameter,
            CornerRadius = new CornerRadius(diameter / 2),
            BorderThickness = new Thickness(1),
            DataContext = node,
            IsHitTestVisible = false
        };
        ring.Classes.Add("wgt-atlas-depth-node");
        ring.Classes.Add(node.IsCore ? "core" : "service");
        ring.Classes.Add(layerClass);
        ApplyThemeClass(ring);

        var point = WorldPoint(node);
        Canvas.SetLeft(ring, point.X - diameter / 2);
        Canvas.SetTop(ring, point.Y - diameter / 2);
        spatialDepthLayer!.Children.Add(ring);
    }

    private void UpdateSpatialDepthSelection()
    {
        if (spatialDepthLayer is not null)
        {
            var selectedId = shell?.SelectedAtlasNode?.NodeId;
            var focusNodeIds = BuildFocusNodeSet(selectedId);
            foreach (var ring in spatialDepthLayer.Children.OfType<Border>())
            {
                if (ring.DataContext is not AtlasNodePresentationViewModel node)
                    continue;

                var selected = string.Equals(node.NodeId, selectedId, StringComparison.Ordinal);
                var contextual = selectedId is not null && !selected && focusNodeIds.Contains(node.NodeId);
                var dimmed = selectedId is not null && !focusNodeIds.Contains(node.NodeId);
                SetStateClass(ring, "selected", selected);
                SetStateClass(ring, "contextual", contextual);
                SetStateClass(ring, "dimmed", dimmed);
            }
        }

        UpdateThemeNodeDecorationSelection();
    }

    private void ApplyThemeToSpatialDepth()
    {
        if (spatialDepthLayer is not null)
        {
            foreach (var ring in spatialDepthLayer.Children.OfType<StyledElement>())
                ApplyThemeClass(ring);
        }

        ApplyThemeToNodeDecorations();
    }
}
