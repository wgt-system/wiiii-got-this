using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopAtlasView
{
    private bool finalVisualExperiencePrepared;
    private ShellViewModel? finalVisualShell;

    private void EnsureFinalVisualExperience()
    {
        if (!finalVisualExperiencePrepared)
        {
            finalVisualExperiencePrepared = true;
            ConfigureFinalChrome();
        }

        AttachFinalVisualShell(experienceShell);
    }

    private void AttachFinalVisualShell(ShellViewModel? next)
    {
        if (ReferenceEquals(finalVisualShell, next))
            return;

        if (finalVisualShell is not null)
            finalVisualShell.PropertyChanged -= OnFinalVisualShellPropertyChanged;

        finalVisualShell = next;
        if (finalVisualShell is not null)
            finalVisualShell.PropertyChanged += OnFinalVisualShellPropertyChanged;

        UpdateFinalOverviewState();
    }

    private void OnFinalVisualShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShellViewModel.SelectedAtlasNode))
            UpdateFinalOverviewState();
    }

    private void ConfigureFinalChrome()
    {
        ControlHint.IsVisible = false;
        ThemeMenuButton.IsVisible = false;

        if (AtlasSearch.Parent is Grid searchGrid &&
            searchGrid.Parent is Border searchDock &&
            searchDock.Parent is StackPanel searchStack)
        {
            searchStack.Width = 430;
            searchStack.Margin = new Thickness(0, 18, 0, 0);
            searchStack.Spacing = 6;
            searchDock.Padding = new Thickness(6, 4);
            AtlasSearch.Height = 34;

            if (searchGrid.Children.OfType<Border>().FirstOrDefault() is { } searchGlyph)
            {
                searchGlyph.Width = 30;
                searchGlyph.Height = 30;
                searchGlyph.CornerRadius = new CornerRadius(15);
            }
        }

        if (ThemeMenuButton.Parent is Canvas settingsCanvas)
        {
            settingsCanvas.Width = 232;
            settingsCanvas.Height = 286;

            var settingsButton = settingsCanvas.Children
                .OfType<Button>()
                .FirstOrDefault(button => !ReferenceEquals(button, ThemeMenuButton));
            if (settingsButton is not null)
            {
                settingsButton.Width = 40;
                settingsButton.Height = 40;
                settingsButton.CornerRadius = new CornerRadius(20);
            }
        }

        InspectorCard.Width = 300;
        InspectorCard.MaxHeight = 560;
        InspectorCard.Padding = new Thickness(16);
    }

    private void UpgradeFinalNodeVisuals()
    {
        foreach (var nodeShell in SceneCanvas.Children.OfType<Border>())
        {
            if (!nodeShell.Classes.Contains("wgt-atlas-node-shell") ||
                nodeShell.Child is not Button button ||
                button.DataContext is not AtlasNodePresentationViewModel node ||
                button.Classes.Contains("final-node-renderer"))
            {
                continue;
            }

            button.Classes.Add("final-node-renderer");
            button.Padding = node.IsCapability ? new Thickness(12, 6) : new Thickness(0);
            button.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            button.VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Stretch;

            if (node.IsCapability)
            {
                nodeShell.Width = 160;
                nodeShell.Height = 48;
                button.Width = 160;
                button.Height = 48;
                button.Content = BuildCapabilityPort(node);
            }
            else
            {
                var width = node.IsCore ? 188d : 148d;
                var height = node.IsCore ? 188d : 156d;
                nodeShell.Width = width;
                nodeShell.Height = height;
                button.Width = width;
                button.Height = height;
                button.Content = BuildProductNode(node);
            }

            RecenterNodeShell(nodeShell, node);
        }

        UpdateFinalOverviewState();
    }

    private void UpdateFinalOverviewState()
    {
        var selectedNode = finalVisualShell?.SelectedAtlasNode;
        var selectedId = selectedNode?.NodeId;
        var focusNodeIds = BuildFocusNodeSet(selectedId);
        var detailContext = selectedNode is not null && !selectedNode.IsCore;

        foreach (var nodeShell in SceneCanvas.Children.OfType<Border>())
        {
            if (!nodeShell.Classes.Contains("wgt-atlas-node-shell") ||
                nodeShell.DataContext is not AtlasNodePresentationViewModel node)
            {
                continue;
            }

            var capabilityExpanded = node.IsCapability && detailContext && focusNodeIds.Contains(node.NodeId);
            if (node.IsCapability && nodeShell.Child is Button capabilityButton)
                SetCapabilityExpansion(nodeShell, capabilityButton, node, capabilityExpanded);

            var secondary = node.IsCapability && !capabilityExpanded;
            SetStateClass(nodeShell, "overview-secondary", secondary);
            if (nodeShell.Child is Button button)
                SetStateClass(button, "overview-secondary", secondary);
        }

        foreach (var path in SceneCanvas.Children.OfType<Avalonia.Controls.Shapes.Path>())
        {
            if (!path.Classes.Contains("wgt-atlas-connection") ||
                path.DataContext is not AtlasConnectionPresentationViewModel connection)
            {
                continue;
            }

            SetStateClass(
                path,
                "overview-secondary",
                selectedNode is null && connection.Kind != AtlasConnectionKind.Composition);
        }
    }

    private static void SetCapabilityExpansion(
        Border nodeShell,
        Button button,
        AtlasNodePresentationViewModel node,
        bool expanded)
    {
        var compact = button.Classes.Contains("compact-port");
        if (expanded && compact)
        {
            button.Classes.Remove("compact-port");
            nodeShell.Classes.Remove("compact-port");
            nodeShell.Width = 160;
            nodeShell.Height = 48;
            button.Width = 160;
            button.Height = 48;
            button.Padding = new Thickness(12, 6);
            button.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            button.Content = BuildCapabilityPort(node);
        }
        else if (!expanded && !compact)
        {
            button.Classes.Add("compact-port");
            nodeShell.Classes.Add("compact-port");
            nodeShell.Width = 32;
            nodeShell.Height = 32;
            button.Width = 32;
            button.Height = 32;
            button.Padding = new Thickness(0);
            button.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            button.Content = BuildCapabilityDot(node);
            ToolTip.SetTip(button, $"{node.Title} · {node.CompactStateText}");
        }

        RecenterNodeShell(nodeShell, node);
    }

    private static void RecenterNodeShell(Border nodeShell, AtlasNodePresentationViewModel node)
    {
        var world = WorldPoint(node);
        Canvas.SetLeft(nodeShell, world.X - nodeShell.Width / 2);
        Canvas.SetTop(nodeShell, world.Y - nodeShell.Height / 2);
    }

    private static StackPanel BuildProductNode(AtlasNodePresentationViewModel node)
    {
        var emblemSize = node.IsCore ? 116d : 78d;
        Control emblemContent;

        if (node.IsCore)
        {
            var glyph = new TextBlock
            {
                Text = "WGT",
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                LetterSpacing = 1.4,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            glyph.Classes.Add("wgt-node-glyph");

            var coreLabel = new TextBlock
            {
                Text = "CORE",
                FontSize = 8,
                FontWeight = FontWeight.SemiBold,
                LetterSpacing = 1.6,
                Opacity = 0.58,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            coreLabel.Classes.Add("wgt-node-core-label");
            emblemContent = new StackPanel
            {
                Spacing = 1,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Children = { glyph, coreLabel }
            };
        }
        else
        {
            emblemContent = ServiceSigilFactory.Create(node.Title, 44);
        }

        var inner = new Border
        {
            Margin = new Thickness(node.IsCore ? 13 : 10),
            Child = emblemContent
        };
        inner.Classes.Add("wgt-node-emblem-inner");
        inner.Classes.Add(node.IsCore ? "core" : "service");

        var emblem = new Border
        {
            Width = emblemSize,
            Height = emblemSize,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Child = inner
        };
        emblem.Classes.Add("wgt-node-emblem");
        emblem.Classes.Add(node.IsCore ? "core" : "service");

        var title = new TextBlock
        {
            Text = node.IsCore ? "Wiiii Got This" : node.Title,
            FontSize = node.IsCore ? 14 : 13,
            FontWeight = FontWeight.SemiBold,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = node.IsCore ? 170 : 142
        };
        title.Classes.Add("wgt-node-title");

        var statusDot = new Border
        {
            Width = 6,
            Height = 6,
            CornerRadius = new CornerRadius(3),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        statusDot.Classes.Add("wgt-atlas-status-dot");
        if (!node.IsAvailable)
            statusDot.Classes.Add("unavailable");

        var status = new TextBlock
        {
            Text = node.CompactStateText,
            FontSize = 8,
            FontWeight = FontWeight.SemiBold,
            LetterSpacing = 0.6,
            Opacity = 0.66,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        status.Classes.Add("wgt-node-status");

        var stateRow = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 5,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Children = { statusDot, status }
        };

        var enterHint = new TextBlock
        {
            Text = node.CanOpenProductSurface ? "ENTER" : string.Empty,
            IsVisible = node.CanOpenProductSurface,
            FontSize = 8,
            FontWeight = FontWeight.SemiBold,
            LetterSpacing = 1.3,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        enterHint.Classes.Add("wgt-node-enter-hint");

        var captionContent = new StackPanel
        {
            Spacing = 1,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Children = { title, stateRow, enterHint }
        };
        var caption = new Border
        {
            Padding = new Thickness(7, 4),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Child = captionContent
        };
        caption.Classes.Add("wgt-node-caption");

        return new StackPanel
        {
            Spacing = 4,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Children = { emblem, caption }
        };
    }

    private static Grid BuildCapabilityPort(AtlasNodePresentationViewModel node)
    {
        var port = new Border
        {
            Width = 7,
            Height = 7,
            CornerRadius = new CornerRadius(4),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        port.Classes.Add("wgt-node-port-dot");

        var title = new TextBlock
        {
            Text = node.Title,
            FontSize = 10,
            FontWeight = FontWeight.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        title.Classes.Add("wgt-node-port-title");

        var state = new TextBlock
        {
            Text = node.CompactStateText,
            FontSize = 7,
            FontWeight = FontWeight.SemiBold,
            LetterSpacing = 0.6,
            Opacity = 0.58,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        state.Classes.Add("wgt-node-port-state");

        var content = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            ColumnSpacing = 8,
            Children = { port, title, state }
        };
        Grid.SetColumn(title, 1);
        Grid.SetColumn(state, 2);
        return content;
    }

    private static Border BuildCapabilityDot(AtlasNodePresentationViewModel node)
    {
        var dot = new Border
        {
            Width = 9,
            Height = 9,
            CornerRadius = new CornerRadius(5),
            DataContext = node,
            IsHitTestVisible = false
        };
        dot.Classes.Add("wgt-node-compact-port-dot");
        if (!node.IsAvailable)
            dot.Classes.Add("unavailable");
        return dot;
    }
}
