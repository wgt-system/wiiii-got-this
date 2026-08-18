using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
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
            experienceShell.AtlasNodes.CollectionChanged -= OnExperienceAtlasNodesChanged;

        experienceShell = next;
        if (experienceShell is not null)
            experienceShell.AtlasNodes.CollectionChanged += OnExperienceAtlasNodesChanged;

        EnsureThemeChooserExperience();
        QueueSpatialDepthRebuild();
        UpdateExperienceState();
    }

    private void OnExperienceAtlasNodesChanged(object? sender, NotifyCollectionChangedEventArgs e) => QueueSpatialDepthRebuild();

    private void QueueSpatialDepthRebuild()
    {
        if (spatialDepthRebuildQueued || experienceShell is null)
            return;

        spatialDepthRebuildQueued = true;
        Dispatcher.UIThread.Post(
            () =>
            {
                spatialDepthRebuildQueued = false;
                if (experienceShell is not null)
                    RebuildSpatialDepthField();
            },
            DispatcherPriority.Render);
    }

    private void EnsureThemeChooserExperience()
    {
        if (themeChooserPrepared)
            return;

        themeChooserPrepared = true;
        if (ThemeMenuButton.Parent is Canvas settingsCanvas)
        {
            settingsCanvas.Width = 356;
            settingsCanvas.Height = 356;
        }

        ThemeChoices.Width = 238;
        ThemeChoices.Orientation = Avalonia.Layout.Orientation.Vertical;
        ThemeChoices.Spacing = 7;
        Canvas.SetRight(ThemeChoices, 52);
        Canvas.SetTop(ThemeChoices, 0);

        themeChooserHeaderText = new TextBlock
        {
            FontSize = 10,
            FontWeight = FontWeight.SemiBold,
            Opacity = 0.72,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        var header = new Border
        {
            Height = 38,
            Padding = new Thickness(12, 0),
            Child = themeChooserHeaderText
        };
        header.Classes.Add("wgt-theme-chooser-header");
        ThemeChoices.Children.Insert(0, header);

        ConfigureThemeChoice(TechnicalThemeButton, "T", "Technical", "System map · precise instrumentation");
        ConfigureThemeChoice(ElegantThemeButton, "E", "Elegant", "Quiet depth · reduced visual noise");
        ConfigureThemeChoice(MachineThemeButton, "M", "Machine", "Engineered frame · circuit emphasis");
        ConfigureThemeChoice(WorldThemeButton, "W", "World", "Spatial terrain · orbital depth cues");
    }

    private static void ConfigureThemeChoice(Button button, string glyph, string title, string description)
    {
        button.Width = 238;
        button.Height = 58;
        button.CornerRadius = new CornerRadius(14);
        button.Padding = new Thickness(9, 7);
        button.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;

        var glyphBorder = new Border
        {
            Width = 34,
            Height = 34,
            CornerRadius = new CornerRadius(17),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = glyph,
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            }
        };
        glyphBorder.Classes.Add("wgt-theme-choice-glyph");

        var text = new StackPanel
        {
            Spacing = 1,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = title,
                    FontSize = 12,
                    FontWeight = FontWeight.SemiBold
                },
                new TextBlock
                {
                    Text = description,
                    FontSize = 9,
                    Opacity = 0.62,
                    TextTrimming = TextTrimming.CharacterEllipsis
                }
            }
        };

        var content = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            ColumnSpacing = 10,
            Children = { glyphBorder, text }
        };
        Grid.SetColumn(text, 1);
        button.Content = content;
    }

    private void UpdateExperienceState()
    {
        if (!themeChooserPrepared)
            return;

        var theme = experienceShell?.AtlasTheme ?? visualTheme;
        if (themeChooserHeaderText is not null)
            themeChooserHeaderText.Text = $"APPEARANCE · {theme.ToString().ToUpperInvariant()}";
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
            var outerDiameter = node.IsCore ? 310d : 226d;
            var innerDiameter = node.IsCore ? 246d : 184d;
            AddDepthRing(node, outerDiameter, "outer");
            AddDepthRing(node, innerDiameter, "inner");
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
        if (spatialDepthLayer is null)
            return;

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

    private void ApplyThemeToSpatialDepth()
    {
        if (spatialDepthLayer is null)
            return;
        foreach (var ring in spatialDepthLayer.Children.OfType<StyledElement>())
            ApplyThemeClass(ring);
    }
}
