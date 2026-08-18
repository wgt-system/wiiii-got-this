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
    private readonly TextBlock hostStatus = new()
    {
        Text = "Orientation is unavailable.",
        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        TextAlignment = Avalonia.Media.TextAlignment.Center,
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        MaxWidth = 500,
        IsVisible = false
    };
    private readonly Uri? productUri;

    public OrientationProductView()
    {
        AutomationProperties.SetName(webView, "Orientation product surface");
        AutomationProperties.SetAutomationId(webView, "OrientationProductSurface");
        AutomationProperties.SetName(hostStatus, "Orientation product status");

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
        var configured = Environment.GetEnvironmentVariable("WGT_ORIENTATION_PRODUCT_URL");
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
            ShowHostError("The Orientation full-product host is not available on this platform yet.");
            return;
        }

        if (productUri is null)
        {
            ShowHostError("The configured Orientation product URL is invalid. Only loopback HTTP(S) URLs are accepted.");
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

        ShowHostError("Orientation could not be loaded. Start the local Orientation standalone UI and try again.");
    }

    private void ShowHostError(string message)
    {
        hostStatus.Text = message;
        AutomationProperties.SetName(hostStatus, message);
        webView.IsVisible = false;
        hostStatus.IsVisible = true;
    }
}
