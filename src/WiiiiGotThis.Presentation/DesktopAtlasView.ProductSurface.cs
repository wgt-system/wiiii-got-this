using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

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
        var returnButton = new Button
        {
            Width = 40,
            Height = 40,
            CornerRadius = new Avalonia.CornerRadius(20),
            Padding = new Avalonia.Thickness(0),
            Content = "◎",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        returnButton.Classes.Add("wgt-product-return");
        ToolTip.SetTip(returnButton, "Return to WGT Atlas");
        AutomationProperties.SetName(returnButton, "Return to WGT Atlas");
        AutomationProperties.SetAutomationId(returnButton, returnAutomationId);
        returnButton.Click += OnReturnFromProductSurface;

        var wgtCaption = new TextBlock
        {
            Text = "WGT",
            FontSize = 8,
            FontWeight = FontWeight.Bold,
            LetterSpacing = 1.3,
            Opacity = 0.62,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        var atlasCaption = new TextBlock
        {
            Text = "ATLAS",
            FontSize = 6,
            FontWeight = FontWeight.SemiBold,
            LetterSpacing = 1.1,
            Opacity = 0.34,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        var returnStack = new StackPanel
        {
            Spacing = 3,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Children = { returnButton, wgtCaption, atlasCaption }
        };

        var serviceMark = new Border
        {
            Width = 42,
            Height = 42,
            Child = ServiceSigilFactory.Create(serviceName, 28)
        };
        serviceMark.Classes.Add("wgt-product-service-mark");
        serviceMark.Classes.Add(serviceName.ToLowerInvariant());

        var serviceLabel = new TextBlock
        {
            Text = serviceName,
            FontSize = 9,
            FontWeight = FontWeight.SemiBold,
            Opacity = 0.8,
            MaxWidth = 58,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };
        var serviceDepth = new TextBlock
        {
            Text = "PRODUCT",
            FontSize = 6,
            FontWeight = FontWeight.SemiBold,
            LetterSpacing = 1,
            Opacity = 0.34,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        var serviceIdentity = new StackPanel
        {
            Spacing = 6,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Children = { serviceMark, serviceLabel, serviceDepth }
        };

        var surfaceCaption = new TextBlock
        {
            Text = "FULL PRODUCT",
            FontSize = 6,
            FontWeight = FontWeight.SemiBold,
            LetterSpacing = 0.8,
            Opacity = 0.28,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        var depthTrack = new Border
        {
            Width = 1,
            Margin = new Avalonia.Thickness(0, 74, 0, 62),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            IsHitTestVisible = false
        };
        depthTrack.Classes.Add("wgt-product-depth-track");
        Grid.SetRowSpan(depthTrack, 3);

        var railGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            Children = { depthTrack, returnStack, serviceIdentity, surfaceCaption }
        };
        Grid.SetRow(returnStack, 0);
        returnStack.Margin = new Avalonia.Thickness(0, 14, 0, 0);
        Grid.SetRow(serviceIdentity, 1);
        Grid.SetRow(surfaceCaption, 2);
        surfaceCaption.Margin = new Avalonia.Thickness(0, 0, 0, 16);

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
            IsIndeterminate = true,
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

    private async void OnReturnFromProductSurface(object? sender, RoutedEventArgs e)
    {
        var activeOverlay = ProductOverlays().FirstOrDefault(candidate => candidate.IsVisible);
        if (activeOverlay is not null)
        {
            activeOverlay.Opacity = 0;
            await Task.Delay(ProductSurfaceTransitionMilliseconds);
            activeOverlay.IsVisible = false;
            activeOverlay.Opacity = 1;
        }

        AtlasViewport.Focus();
        e.Handled = true;
    }
}
