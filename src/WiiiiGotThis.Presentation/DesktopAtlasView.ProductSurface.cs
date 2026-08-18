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
            vocationProductView!.Reload();
        }
        else if (string.Equals(serviceId, "illumination", StringComparison.Ordinal))
        {
            await OpenIlluminationProductSurfaceAsync();
        }
        else if (string.Equals(serviceId, "orientation", StringComparison.Ordinal))
        {
            EnsureOrientationProductOverlay();
            ShowOnlyProductOverlay(orientationProductOverlay!);
            orientationProductView!.Reload();
        }

        e.Handled = true;
    }

    private void EnsureVocationProductOverlay()
    {
        if (vocationProductOverlay is not null)
            return;

        vocationProductView = new VocationProductView();
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

        orientationProductView = new OrientationProductView();
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

        var serviceLabel = new TextBlock
        {
            Text = serviceName,
            FontSize = 11,
            FontWeight = FontWeight.SemiBold,
            Opacity = 0.72,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };

        var rail = new Border
        {
            Width = 68,
            Background = new SolidColorBrush(Color.Parse("#FF111318")),
            BorderBrush = new SolidColorBrush(Color.Parse("#FF2B2F38")),
            BorderThickness = new Avalonia.Thickness(0, 0, 1, 0),
            Child = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto"),
                Children =
                {
                    returnButton,
                    serviceLabel
                }
            }
        };
        rail.Classes.Add("wgt-product-rail");
        Grid.SetRow(returnButton, 0);
        returnButton.Margin = new Avalonia.Thickness(0, 16, 0, 0);
        Grid.SetRow(serviceLabel, 2);
        serviceLabel.Margin = new Avalonia.Thickness(6, 0, 6, 18);

        var contentHost = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#FF0E1014")),
            Child = productContent
        };
        Grid.SetColumn(contentHost, 1);

        var overlay = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("68,*"),
            Background = new SolidColorBrush(Color.Parse("#FF0E1014")),
            IsVisible = false,
            Children =
            {
                rail,
                contentHost
            }
        };
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
        AtlasSearch.Focus();
        e.Handled = true;
    }
}
