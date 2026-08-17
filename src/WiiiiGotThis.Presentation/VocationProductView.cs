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
    private readonly TextBlock hostStatus = new()
    {
        Text = "Vocation is unavailable.",
        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        TextAlignment = Avalonia.Media.TextAlignment.Center,
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        MaxWidth = 460,
        IsVisible = false
    };
    private readonly Uri? productUri;

    public VocationProductView()
    {
        AutomationProperties.SetName(webView, "Vocation product surface");
        AutomationProperties.SetAutomationId(webView, "VocationProductSurface");
        AutomationProperties.SetName(hostStatus, "Vocation product status");

        productUri = ResolveProductUri();
        Children.Add(webView);
        Children.Add(hostStatus);

        webView.AdapterCreated += OnAdapterCreated;
        webView.NavigationStarted += OnNavigationStarted;
        webView.NavigationCompleted += OnNavigationCompleted;
    }

    public void Reload()
    {
        if (productUri is not null)
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

        if (productUri is null)
        {
            ShowHostError("The configured Vocation product URL is invalid. Only loopback HTTP(S) URLs are accepted.");
            return;
        }

        webView.Source = productUri;
    }

    private void OnNavigationStarted(object? sender, WebViewNavigationStartingEventArgs e)
    {
        hostStatus.IsVisible = false;
        webView.IsVisible = true;
    }

    private void OnNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            hostStatus.IsVisible = false;
            webView.IsVisible = true;
            return;
        }

        ShowHostError("Vocation could not be loaded. Start or restore the local Vocation provider and try again.");
    }

    private void ShowHostError(string message)
    {
        hostStatus.Text = message;
        AutomationProperties.SetName(hostStatus, message);
        webView.IsVisible = false;
        hostStatus.IsVisible = true;
    }
}
