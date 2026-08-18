using Avalonia.Automation;
using Avalonia.Controls;

namespace WiiiiGotThis.Presentation;

/// <summary>
/// Provider-specific host for Vocation's complete browser product on Windows.
/// This is intentionally not a generic WGT plugin or Product Surface contract.
/// </summary>
public sealed class VocationProductView : Grid
{
    private static readonly Uri DefaultProductUri = new("http://127.0.0.1:8765/");

    private readonly NativeWebView webView = new();
    private readonly ProgressBar progress = new()
    {
        IsIndeterminate = true,
        Height = 4,
        Width = 260
    };
    private readonly TextBlock hostStatus = new()
    {
        Text = "Starting Vocation…",
        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        TextAlignment = Avalonia.Media.TextAlignment.Center,
        MaxWidth = 500
    };
    private readonly Button retry = new()
    {
        Content = "Retry Vocation",
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
        IsVisible = false
    };
    private readonly StackPanel statusPanel;
    private readonly Uri? productUri;

    public VocationProductView()
    {
        AutomationProperties.SetName(webView, "Vocation product surface");
        AutomationProperties.SetAutomationId(webView, "VocationProductSurface");
        AutomationProperties.SetName(hostStatus, "Vocation product status");

        statusPanel = new StackPanel
        {
            Spacing = 12,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Children = { progress, hostStatus, retry }
        };
        retry.Click += (_, _) => Reload();

        productUri = ResolveProductUri();
        Children.Add(webView);
        Children.Add(statusPanel);

        webView.AdapterCreated += OnAdapterCreated;
        webView.NavigationStarted += OnNavigationStarted;
        webView.NavigationCompleted += OnNavigationCompleted;
        ShowLoading("Starting local Vocation product…");
    }

    public void Reload()
    {
        if (productUri is null)
        {
            ShowHostError("The configured Vocation product URL is invalid. Only loopback HTTP(S) URLs are accepted.");
            return;
        }

        ShowLoading("Connecting to local Vocation…");
        webView.Source = productUri;
    }

    private static Uri? ResolveProductUri()
    {
        var configured = Environment.GetEnvironmentVariable("WGT_VOCATION_PRODUCT_URL");
        if (string.IsNullOrWhiteSpace(configured))
            return DefaultProductUri;
        if (!Uri.TryCreate(configured.Trim(), UriKind.Absolute, out var candidate))
            return null;
        if (!candidate.IsLoopback || (candidate.Scheme != Uri.UriSchemeHttp && candidate.Scheme != Uri.UriSchemeHttps))
            return null;
        return candidate;
    }

    private void OnAdapterCreated(object? sender, WebViewAdapterEventArgs e)
    {
        if (!OperatingSystem.IsWindows())
        {
            ShowHostError("The Vocation full-product host is not available on this platform yet.");
            return;
        }

        Reload();
    }

    private void OnNavigationStarted(object? sender, WebViewNavigationStartingEventArgs e) =>
        ShowLoading("Loading Vocation…");

    private void OnNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            statusPanel.IsVisible = false;
            webView.IsVisible = true;
            return;
        }

        ShowHostError("Vocation is not running at its local product address. Start the Vocation provider, then retry.");
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
