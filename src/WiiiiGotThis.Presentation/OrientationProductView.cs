using Avalonia.Automation;
using Avalonia.Controls;

namespace WiiiiGotThis.Presentation;

/// <summary>
/// Provider-specific Windows host for Orientation's standalone browser product.
/// The reusable Orientation map embed remains a separate narrow capability surface.
/// </summary>
public sealed class OrientationProductView : Grid
{
    private static readonly Uri DefaultProductUri = new("http://127.0.0.1:5173/app.html");

    private readonly NativeWebView webView = new();
    private readonly ProgressBar progress = new()
    {
        IsIndeterminate = true,
        Height = 4,
        Width = 260
    };
    private readonly TextBlock hostStatus = new()
    {
        Text = "Starting Orientation…",
        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        TextAlignment = Avalonia.Media.TextAlignment.Center,
        MaxWidth = 520
    };
    private readonly Button retry = new()
    {
        Content = "Retry Orientation",
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
        IsVisible = false
    };
    private readonly StackPanel statusPanel;
    private readonly Uri? productUri;
    private bool reloadInProgress;

    public OrientationProductView(IOrientationProductRuntime? runtime = null)
    {
        Runtime = runtime;
        AutomationProperties.SetName(webView, "Orientation product surface");
        AutomationProperties.SetAutomationId(webView, "OrientationProductSurface");
        AutomationProperties.SetName(hostStatus, "Orientation product status");

        statusPanel = new StackPanel
        {
            Spacing = 12,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Children = { progress, hostStatus, retry }
        };
        retry.Click += OnRetry;

        productUri = ResolveProductUri();
        Children.Add(webView);
        Children.Add(statusPanel);

        webView.AdapterCreated += OnAdapterCreated;
        webView.NavigationStarted += OnNavigationStarted;
        webView.NavigationCompleted += OnNavigationCompleted;
        ShowLoading("Preparing local Orientation product…");
    }

    public IOrientationProductRuntime? Runtime { get; set; }

    public async Task ReloadAsync()
    {
        if (reloadInProgress)
            return;
        if (productUri is null)
        {
            ShowHostError("The configured Orientation product URL is invalid. Only loopback HTTP(S) URLs are accepted.");
            return;
        }

        reloadInProgress = true;
        try
        {
            ShowLoading("Starting local Orientation…");
            if (Runtime is not null)
            {
                var readiness = await Runtime.EnsureReadyAsync(productUri);
                if (!readiness.IsReady)
                {
                    ShowHostError(readiness.FailureMessage ?? "Orientation could not be started.");
                    return;
                }
            }

            ShowLoading("Loading Orientation…");
            webView.Source = productUri;
        }
        catch (Exception ex)
        {
            ShowHostError($"Orientation could not be prepared: {ex.Message}");
        }
        finally
        {
            reloadInProgress = false;
        }
    }

    private static Uri? ResolveProductUri()
    {
        var configured = Environment.GetEnvironmentVariable("WGT_ORIENTATION_PRODUCT_URL");
        if (string.IsNullOrWhiteSpace(configured))
            return DefaultProductUri;
        if (!Uri.TryCreate(configured.Trim(), UriKind.Absolute, out var candidate))
            return null;
        if (!candidate.IsLoopback || (candidate.Scheme != Uri.UriSchemeHttp && candidate.Scheme != Uri.UriSchemeHttps))
            return null;
        return candidate;
    }

    private async void OnAdapterCreated(object? sender, WebViewAdapterEventArgs e)
    {
        if (!OperatingSystem.IsWindows())
        {
            ShowHostError("The Orientation full-product host is not available on this platform yet.");
            return;
        }

        await ReloadAsync();
    }

    private async void OnRetry(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ReloadAsync();
        e.Handled = true;
    }

    private void OnNavigationStarted(object? sender, WebViewNavigationStartingEventArgs e) =>
        ShowLoading("Loading Orientation…");

    private void OnNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            statusPanel.IsVisible = false;
            webView.IsVisible = true;
            return;
        }

        ShowHostError("Orientation started, but its local standalone surface could not be loaded. Retry the provider connection.");
    }

    private void ShowLoading(string message)
    {
        hostStatus.Text = message;
        progress.IsVisible = true;
        retry.IsVisible = false;
        webView.IsVisible = false;
        statusPanel.IsVisible = true;
        AutomationProperties.SetName(hostStatus, message);
    }

    private void ShowHostError(string message)
    {
        hostStatus.Text = message;
        progress.IsVisible = false;
        retry.IsVisible = true;
        webView.IsVisible = false;
        statusPanel.IsVisible = true;
        AutomationProperties.SetName(hostStatus, message);
    }
}
