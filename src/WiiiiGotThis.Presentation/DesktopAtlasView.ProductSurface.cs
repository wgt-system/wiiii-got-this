using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopAtlasView
{
    private const int ProductSurfaceTransitionMilliseconds = 170;

    private Grid? vocationProductOverlay;
    private VocationProductView? vocationProductView;
    private Grid? orientationProductOverlay;
    private OrientationProductView? orientationProductView;
    private Grid? illuminationProductOverlay;
    private ContentControl? illuminationProductContent;
    private Control? illuminationProductSurface;
    private bool illuminationProductSurfaceLoading;

    public IIlluminationProductSurfaceSource? IlluminationProductSurfaceSource { get; set; }
    public IVocationProductRuntime? VocationProductRuntime { get; set; }
    public IOrientationProductRuntime? OrientationProductRuntime { get; set; }

    private async void OnOpenProductSurface(object? sender, RoutedEventArgs e)
    {
        if (await OpenSelectedProductSurfaceAsync())
            e.Handled = true;
    }

    private async Task<bool> OpenSelectedProductSurfaceAsync()
    {
        var selectedNode = shell?.SelectedAtlasNode;
        var serviceId = selectedNode?.ServiceIdentity?.Value;
        if (selectedNode?.CanOpenProductSurface != true || string.IsNullOrWhiteSpace(serviceId) || shell is null)
            return false;

        if (!selectedNode.IsEnabled && shell.EnableOnThisDeviceCommand.CanExecute(null))
            await shell.EnableOnThisDeviceCommand.ExecuteAsync(null);

        if (string.Equals(serviceId, "vocation", StringComparison.Ordinal))
        {
            EnsureVocationProductOverlay();
            ShowOnlyProductOverlay(vocationProductOverlay!);
            await vocationProductView!.ReloadAsync();
            return true;
        }

        if (string.Equals(serviceId, "illumination", StringComparison.Ordinal))
        {
            await OpenIlluminationProductSurfaceAsync();
            return true;
        }

        if (string.Equals(serviceId, "orientation", StringComparison.Ordinal))
        {
            EnsureOrientationProductOverlay();
            ShowOnlyProductOverlay(orientationProductOverlay!);
            await orientationProductView!.ReloadAsync();
            return true;
        }

        return false;
    }

    private void EnsureVocationProductOverlay()
    {
        if (vocationProductOverlay is not null)
            return;

        vocationProductView = new VocationProductView(VocationProductRuntime);
        vocationProductOverlay = CreateProductOverlay(
            vocationProductView,
            "Vocation",
            "Vocation in Wiiii Got This",
            "ReturnToAtlasFromVocation");
        AtlasViewport.Children.Add(vocationProductOverlay);
    }

    private void EnsureOrientationProductOverlay()
    {
        if (orientationProductOverlay is not null)
            return;

        orientationProductView = new OrientationProductView(OrientationProductRuntime);
        orientationProductOverlay = CreateProductOverlay(
            orientationProductView,
            "Orientation",
            "Orientation in Wiiii Got This",
            "ReturnToAtlasFromOrientation");
        AtlasViewport.Children.Add(orientationProductOverlay);
    }

    private async Task OpenIlluminationProductSurfaceAsync()
    {
        EnsureIlluminationProductOverlay();
        ShowOnlyProductOverlay(illuminationProductOverlay!);

        if (illuminationProductSurface is not null || illuminationProductSurfaceLoading)
            return;

        await LoadIlluminationProductSurfaceAsync();
    }

    private void EnsureIlluminationProductOverlay()
    {
        if (illuminationProductOverlay is not null)
            return;

        illuminationProductContent = new ContentControl
        {
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            Content = BuildIlluminationLoadingState()
        };
        illuminationProductOverlay = CreateProductOverlay(
            illuminationProductContent,
            "Illumination",
            "Illumination in Wiiii Got This",
            "ReturnToAtlasFromIllumination");
        AtlasViewport.Children.Add(illuminationProductOverlay);
    }

    private async Task LoadIlluminationProductSurfaceAsync()
    {
        if (illuminationProductContent is null || illuminationProductSurfaceLoading)
            return;

        var source = IlluminationProductSurfaceSource;
        if (source is null)
        {
            illuminationProductContent.Content = BuildIlluminationErrorState(
                "Illumination is not configured for this Windows build.");
            return;
        }

        illuminationProductSurfaceLoading = true;
        illuminationProductContent.Content = BuildIlluminationLoadingState();
        try
        {
            illuminationProductSurface = await source.CreateAsync();
            illuminationProductContent.Content = illuminationProductSurface;
        }
        catch (Exception)
        {
            illuminationProductSurface = null;
            illuminationProductContent.Content = BuildIlluminationErrorState(
                "Illumination could not start. Its local data was not handed to WGT.");
        }
        finally
        {
            illuminationProductSurfaceLoading = false;
        }
    }

    private Grid CreateProductOverlay(
        Control productContent,
        string serviceName,
        string automationName,
        string returnAutomationId)
    {
        var returnButton = BuildRailButton(
            "◎",
            "Return to WGT Atlas",
            returnAutomationId,
            OnReturnFromProductSurface);

        var settingsButton = BuildRailButton(
            "⚙",
            "WGT settings",
            $"ProductRailWgtSettings{serviceName}",
            OnProductRailWgtSettings);

        var wgtCaption = new TextBlock
        {
            Text = "WGT",
            FontSize = 8,
            FontWeight = FontWeight.Bold,
            LetterSpacing = 1.3,
            Opacity = 0.62,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        var wgtTools = new StackPanel
        {
            Spacing = 8,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Children = { returnButton, wgtCaption, settingsButton }
        };

        var divider = new Border
        {
            Height = 1,
            Width = 32,
            Margin = new Avalonia.Thickness(0, 4),
            Background = new SolidColorBrush(Color.FromArgb(48, 126, 173, 153)),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        var serviceMark = new Border
        {
            Width = 42,
            Height = 42,
            Child = ServiceSigilFactory.Create(serviceName, 28)
        };
        serviceMark.Classes.Add("wgt-product-service-mark");
        serviceMark.Classes.Add(serviceName.ToLowerInvariant());
        ToolTip.SetTip(serviceMark, serviceName);
        AutomationProperties.SetName(serviceMark, serviceName);

        var serviceLabel = new TextBlock
        {
            Text = serviceName,
            FontSize = 8,
            FontWeight = FontWeight.SemiBold,
            Opacity = 0.72,
            MaxWidth = 58,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };

        var providerHeader = new StackPanel
        {
            Spacing = 5,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Children = { serviceMark, serviceLabel }
        };

        var capabilityActions = BuildProviderCapabilityRail(serviceName);
        var capabilityScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = capabilityActions,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        AutomationProperties.SetName(capabilityScroll, $"{serviceName} capability controls");

        var providerSection = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
            RowSpacing = 8,
            Children = { providerHeader, capabilityScroll }
        };
        Grid.SetRow(capabilityScroll, 1);

        var depthTrack = new Border
        {
            Width = 1,
            Margin = new Avalonia.Thickness(0, 82, 0, 18),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            IsHitTestVisible = false
        };
        depthTrack.Classes.Add("wgt-product-depth-track");
        Grid.SetRowSpan(depthTrack, 3);

        var railGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,*"),
            Children = { depthTrack, wgtTools, divider, providerSection }
        };
        Grid.SetRow(wgtTools, 0);
        wgtTools.Margin = new Avalonia.Thickness(0, 14, 0, 8);
        Grid.SetRow(divider, 1);
        Grid.SetRow(providerSection, 2);
        providerSection.Margin = new Avalonia.Thickness(0, 8, 0, 14);

        var rail = new Border
        {
            Width = 68,
            Child = railGrid
        };
        rail.Classes.Add("wgt-product-rail");

        var contentHost = new Border
        {
            Child = productContent,
            ClipToBounds = true
        };
        contentHost.Classes.Add("wgt-product-stage");
        Grid.SetColumn(contentHost, 1);

        var overlay = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("68,*"),
            IsVisible = false,
            Opacity = 1,
            ClipToBounds = true,
            Children = { rail, contentHost }
        };
        overlay.Classes.Add("wgt-product-overlay");
        AutomationProperties.SetName(overlay, automationName);
        return overlay;
    }

    private StackPanel BuildProviderCapabilityRail(string serviceName)
    {
        var stack = new StackPanel
        {
            Spacing = 7,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        var serviceId = serviceName.ToLowerInvariant();
        var capabilities = shell?.AtlasNodes
            .Where(node =>
                node.IsCapability
                && string.Equals(node.ServiceIdentity?.Value, serviceId, StringComparison.Ordinal))
            .OrderBy(node => node.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(node => node.NodeId, StringComparer.Ordinal)
            .ToArray() ?? [];

        foreach (var capability in capabilities)
        {
            var glyph = capability.CapabilityIdentity?.Value switch
            {
                BuildAtlasProjectionUseCase.OrientationGeospatialCapabilityId => "⌖",
                BuildAtlasProjectionUseCase.ConveyanceDurableDeliveryCapabilityId => "⇄",
                _ => "◇"
            };
            var button = BuildRailButton(
                glyph,
                capability.Title,
                $"ProductRailCapability{serviceName}{capability.CapabilityIdentity?.Value}",
                OnProductRailCapability);
            button.Tag = capability;
            stack.Children.Add(button);
        }

        if (capabilities.Length == 0)
        {
            var quietMark = new TextBlock
            {
                Text = "·",
                FontSize = 16,
                Opacity = 0.24,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            ToolTip.SetTip(quietMark, "No WGT-level provider capabilities are exposed here yet");
            stack.Children.Add(quietMark);
        }

        return stack;
    }

    private static Button BuildRailButton(
        string glyph,
        string tooltip,
        string automationId,
        EventHandler<RoutedEventArgs> handler)
    {
        var button = new Button
        {
            Width = 38,
            Height = 38,
            CornerRadius = new Avalonia.CornerRadius(19),
            Padding = new Avalonia.Thickness(0),
            Content = glyph,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        button.Classes.Add("wgt-product-rail-action");
        ToolTip.SetTip(button, tooltip);
        AutomationProperties.SetName(button, tooltip);
        AutomationProperties.SetAutomationId(button, automationId);
        button.Click += handler;
        return button;
    }

    private async void OnProductRailWgtSettings(object? sender, RoutedEventArgs e)
    {
        await HideActiveProductOverlayAsync();
        if (shell is not null)
            shell.AtlasSettingsExpanded = true;
        AtlasViewport.Focus();
        e.Handled = true;
    }

    private async void OnProductRailCapability(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: AtlasNodePresentationViewModel capability } || shell is null)
            return;

        await HideActiveProductOverlayAsync();
        shell.SelectAtlasNodeCommand.Execute(capability);
        CenterOnSelected();
        AtlasViewport.Focus();
        e.Handled = true;
    }

    private void ShowOnlyProductOverlay(Grid overlay)
    {
        foreach (var candidate in ProductOverlays())
        {
            if (ReferenceEquals(candidate, overlay))
                continue;
            candidate.IsVisible = false;
            candidate.Opacity = 1;
        }

        overlay.Opacity = 0;
        overlay.IsVisible = true;
        Dispatcher.UIThread.Post(
            () =>
            {
                if (overlay.IsVisible)
                    overlay.Opacity = 1;
            },
            DispatcherPriority.Render);
    }

    private IEnumerable<Grid> ProductOverlays()
    {
        if (vocationProductOverlay is not null)
            yield return vocationProductOverlay;
        if (orientationProductOverlay is not null)
            yield return orientationProductOverlay;
        if (illuminationProductOverlay is not null)
            yield return illuminationProductOverlay;
    }

    private StackPanel BuildIlluminationErrorState(string message)
    {
        var retry = new Button
        {
            Content = "Retry Illumination",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        retry.Classes.Add("wgt-provider-retry");
        retry.Click += OnRetryIllumination;

        var panel = BuildIlluminationStatusShell(message, retry, isLoading: false);
        AutomationProperties.SetName(panel, "Illumination unavailable");
        return panel;
    }

    private static StackPanel BuildIlluminationLoadingState() =>
        BuildIlluminationStatusShell("Starting local Illumination…", null, isLoading: true);

    private static StackPanel BuildIlluminationStatusShell(string message, Button? retry, bool isLoading)
    {
        var mark = new Border
        {
            Width = 64,
            Height = 64,
            Child = ServiceSigilFactory.Create("Illumination", 42)
        };
        mark.Classes.Add("wgt-provider-status-mark");
        mark.Classes.Add("illumination");

        var title = new TextBlock
        {
            Text = "Illumination",
            FontSize = 17,
            FontWeight = FontWeight.SemiBold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        title.Classes.Add("wgt-provider-status-title");

        var type = new TextBlock
        {
            Text = "PROVIDER PRODUCT",
            FontSize = 7,
            FontWeight = FontWeight.SemiBold,
            LetterSpacing = 1.4,
            Opacity = 0.42,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        var progress = new ProgressBar
        {
            Height = 3,
            Width = 220,
            IsVisible = isLoading
        };
        progress.Classes.Add("wgt-provider-status-progress");

        var status = new TextBlock
        {
            Text = message,
            MaxWidth = 430,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        status.Classes.Add("wgt-provider-status-text");

        var panel = new StackPanel
        {
            Width = 460,
            Spacing = 10,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Children = { mark, title, type, progress, status }
        };
        panel.Classes.Add("wgt-provider-status-panel");
        if (retry is not null)
            panel.Children.Add(retry);
        return panel;
    }

    private async void OnRetryIllumination(object? sender, RoutedEventArgs e)
    {
        await LoadIlluminationProductSurfaceAsync();
        e.Handled = true;
    }

    private async Task HideActiveProductOverlayAsync()
    {
        var activeOverlay = ProductOverlays().FirstOrDefault(candidate => candidate.IsVisible);
        if (activeOverlay is null)
            return;

        activeOverlay.Opacity = 0;
        var delay = shell?.IsAtlasReducedMotion == true
            ? 0
            : ProductSurfaceTransitionMilliseconds;
        if (delay > 0)
            await Task.Delay(delay);
        activeOverlay.IsVisible = false;
        activeOverlay.Opacity = 1;
    }

    private async void OnReturnFromProductSurface(object? sender, RoutedEventArgs e)
    {
        await HideActiveProductOverlayAsync();
        AtlasViewport.Focus();
        e.Handled = true;
    }
}
