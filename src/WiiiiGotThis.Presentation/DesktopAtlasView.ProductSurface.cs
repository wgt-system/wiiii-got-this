using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopAtlasView
{
    private Grid? vocationProductOverlay;
    private VocationProductView? vocationProductView;
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
            vocationProductOverlay!.IsVisible = true;
            vocationProductView!.Reload();
        }
        else if (string.Equals(serviceId, "illumination", StringComparison.Ordinal))
        {
            await OpenIlluminationProductSurfaceAsync();
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
            "Vocation in Wiiii Got This",
            "ReturnToAtlasFromVocation");
        AtlasViewport.Children.Add(vocationProductOverlay);
    }

    private async Task OpenIlluminationProductSurfaceAsync()
    {
        EnsureIlluminationProductOverlay();
        illuminationProductOverlay!.IsVisible = true;

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

    private Grid CreateProductOverlay(Control productContent, string automationName, string returnAutomationId)
    {
        var returnButton = new Button
        {
            Width = 46,
            Height = 46,
            CornerRadius = new Avalonia.CornerRadius(23),
            Padding = new Avalonia.Thickness(0),
            Content = "◎",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Margin = new Avalonia.Thickness(18)
        };
        returnButton.Classes.Add("wgt-product-return");
        ToolTip.SetTip(returnButton, "Return to WGT Atlas");
        AutomationProperties.SetName(returnButton, "Return to WGT Atlas");
        AutomationProperties.SetAutomationId(returnButton, returnAutomationId);
        returnButton.Click += OnReturnFromProductSurface;

        var overlay = new Grid
        {
            Background = new SolidColorBrush(Colors.Transparent),
            IsVisible = false,
            Children =
            {
                productContent,
                returnButton
            }
        };
        AutomationProperties.SetName(overlay, automationName);
        return overlay;
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
        if (illuminationProductOverlay is not null)
            illuminationProductOverlay.IsVisible = false;
        AtlasSearch.Focus();
        e.Handled = true;
    }
}
