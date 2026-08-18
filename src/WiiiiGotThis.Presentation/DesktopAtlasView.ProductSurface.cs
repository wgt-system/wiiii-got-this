using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopAtlasView
{
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
        var selectedNode = shell?.SelectedAtlasNode;
        var serviceId = selectedNode?.ServiceIdentity?.Value;
        if (selectedNode?.CanOpenProductSurface != true || string.IsNullOrWhiteSpace(serviceId) || shell is null)
            return;

        if (!selectedNode.IsEnabled && shell.EnableOnThisDeviceCommand.CanExecute(null))
            await shell.EnableOnThisDeviceCommand.ExecuteAsync(null);

        if (string.Equals(serviceId, "vocation", StringComparison.Ordinal))
        {
            EnsureVocationProductOverlay();
            ShowOnlyProductOverlay(vocationProductOverlay!);
            await vocationProductView!.ReloadAsync();
        }
        else if (string.Equals(serviceId, "illumination", StringComparison.Ordinal))
        {
            await OpenIlluminationProductSurfaceAsync();
        }
        else if (string.Equals(serviceId, "orientation", StringComparison.Ordinal))
        {
            EnsureOrientationProductOverlay();
            ShowOnlyProductOverlay(orientationProductOverlay!);
            await orientationProductView!.ReloadAsync();
        }

        e.Handled = true;
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
            Width = 44,
            Height = 44,
            CornerRadius = new Avalonia.CornerRadius(22),
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
            FontSize = 9,
            FontWeight = FontWeight.SemiBold,
            Opacity = 0.62,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        var returnStack = new StackPanel
        {
            Spacing = 5,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Children = { returnButton, wgtCaption }
        };

        var serviceMark = new Border
        {
            Width = 36,
            Height = 36,
            Child = new TextBlock
            {
                Text = serviceName[0].ToString(),
                FontWeight = FontWeight.SemiBold,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            }
        };
        serviceMark.Classes.Add("wgt-product-service-mark");

        var serviceLabel = new TextBlock
        {
            Text = serviceName,
            FontSize = 11,
            FontWeight = FontWeight.SemiBold,
            Opacity = 0.82,
            MaxWidth = 64,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };
        var serviceIdentity = new StackPanel
        {
            Spacing = 8,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Children = { serviceMark, serviceLabel }
        };

        var surfaceCaption = new TextBlock
        {
            Text = "SERVICE",
            FontSize = 8,
            FontWeight = FontWeight.SemiBold,
            Opacity = 0.42,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        var railGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            Children = { returnStack, serviceIdentity, surfaceCaption }
        };
        Grid.SetRow(returnStack, 0);
        returnStack.Margin = new Avalonia.Thickness(0, 16, 0, 0);
        Grid.SetRow(serviceIdentity, 1);
        Grid.SetRow(surfaceCaption, 2);
        surfaceCaption.Margin = new Avalonia.Thickness(0, 0, 0, 18);

        var rail = new Border
        {
            Width = 76,
            Child = railGrid
        };
        rail.Classes.Add("wgt-product-rail");

        var contentHost = new Border
        {
            Child = productContent
        };
        contentHost.Classes.Add("wgt-product-stage");
        Grid.SetColumn(contentHost, 1);

        var overlay = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("76,*"),
            IsVisible = false,
            Children = { rail, contentHost }
        };
        overlay.Classes.Add("wgt-product-overlay");
        AutomationProperties.SetName(overlay, automationName);
        return overlay;
    }

    private void ShowOnlyProductOverlay(Grid overlay)
    {
        if (vocationProductOverlay is not null)
            vocationProductOverlay.IsVisible = ReferenceEquals(vocationProductOverlay, overlay);
        if (orientationProductOverlay is not null)
            orientationProductOverlay.IsVisible = ReferenceEquals(orientationProductOverlay, overlay);
        if (illuminationProductOverlay is not null)
            illuminationProductOverlay.IsVisible = ReferenceEquals(illuminationProductOverlay, overlay);
    }

    private StackPanel BuildIlluminationErrorState(string message)
    {
        var retry = new Button
        {
            Content = "Retry Illumination",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        retry.Click += OnRetryIllumination;

        return new StackPanel
        {
            Width = 420,
            Spacing = 12,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = "Illumination unavailable",
                    FontSize = 22,
                    FontWeight = FontWeight.SemiBold,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                },
                new TextBlock
                {
                    Text = message,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                },
                retry
            }
        };
    }

    private static StackPanel BuildIlluminationLoadingState() => new()
    {
        Width = 320,
        Spacing = 12,
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        Children =
        {
            new ProgressBar { IsIndeterminate = true, Height = 4 },
            new TextBlock
            {
                Text = "Starting Illumination…",
                TextAlignment = TextAlignment.Center
            }
        }
    };

    private async void OnRetryIllumination(object? sender, RoutedEventArgs e)
    {
        await LoadIlluminationProductSurfaceAsync();
        e.Handled = true;
    }

    private void OnReturnFromProductSurface(object? sender, RoutedEventArgs e)
    {
        if (vocationProductOverlay is not null)
            vocationProductOverlay.IsVisible = false;
        if (orientationProductOverlay is not null)
            orientationProductOverlay.IsVisible = false;
        if (illuminationProductOverlay is not null)
            illuminationProductOverlay.IsVisible = false;
        AtlasViewport.Focus();
        e.Handled = true;
    }
}
