using System.ComponentModel;
using System.Text.Json;
using Avalonia.Automation;
using Avalonia.Controls;

namespace WiiiiGotThis.Presentation;

public sealed class WgtMapView : Grid, IDisposable
{
    private readonly NativeWebView webView = new();
    private readonly TextBlock hostStatus = new()
    {
        Text = "Orientation map host is unavailable.",
        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        TextAlignment = Avalonia.Media.TextAlignment.Center,
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        MaxWidth = 420,
        IsVisible = false,
    };

    private VocationMapProjectionViewModel? viewModel;
    private string? orientationEmbedPath;
    private bool bridgeReady;
    private bool disposed;

    public WgtMapView()
    {
        AutomationProperties.SetName(webView, "Opportunity map rendered by Orientation");
        AutomationProperties.SetAutomationId(webView, "OrientationMap");
        AutomationProperties.SetName(hostStatus, "Orientation map status");

        Children.Add(webView);
        Children.Add(hostStatus);

        webView.AdapterCreated += OnAdapterCreated;
        webView.WebMessageReceived += OnWebMessageReceived;
        webView.NavigationStarted += OnNavigationStarted;
        webView.NavigationCompleted += OnNavigationCompleted;
        DataContextChanged += OnDataContextChanged;

        PrepareOrientationHost();
    }

    private void PrepareOrientationHost()
    {
        var configuredPath = Environment.GetEnvironmentVariable("WGT_ORIENTATION_EMBED_PATH");
        var embedPath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(AppContext.BaseDirectory, "orientation-map", "embed.html")
            : configuredPath.Trim();

        if (!File.Exists(embedPath))
        {
            ShowHostError($"Orientation map host was not found at {embedPath}.");
            return;
        }

        try
        {
            orientationEmbedPath = Path.GetFullPath(embedPath);
        }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or PathTooLongException)
        {
            ShowHostError("Orientation map host path is invalid.");
        }
    }

    private void OnAdapterCreated(object? sender, WebViewAdapterEventArgs e)
    {
        if (orientationEmbedPath is null)
            return;

        if (!OperatingSystem.IsWindows())
        {
            ShowHostError("The Orientation desktop map host is not available on this platform yet.");
            return;
        }

        if (!WindowsOrientationWebViewHost.TryConfigure(e.TryGetPlatformHandle(), orientationEmbedPath, out var error))
        {
            ShowHostError(error ?? "Orientation map host could not be configured.");
            return;
        }

        webView.Source = WindowsOrientationWebViewHost.EmbedUri;
    }

    private void OnNavigationStarted(object? sender, WebViewNavigationStartingEventArgs e)
    {
        bridgeReady = false;
    }

    private void OnNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
            ShowHostError("Orientation map host could not be loaded.");
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (viewModel is not null)
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        viewModel = DataContext as VocationMapProjectionViewModel;
        if (viewModel is not null)
            viewModel.PropertyChanged += OnViewModelPropertyChanged;

        _ = SendSceneIfReadyAsync();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VocationMapProjectionViewModel.State))
            _ = SendSceneIfReadyAsync();
    }

    private void OnWebMessageReceived(object? sender, WebMessageReceivedEventArgs e)
    {
        if (!OrientationMapBridgeAdapter.TryParseOutboundMessage(e.Body, out var message) || message is null)
        {
            ShowHostError("Orientation returned an invalid host message.");
            return;
        }

        switch (message.Type)
        {
            case "bridge.ready":
                bridgeReady = true;
                HideHostError();
                _ = SendSceneIfReadyAsync();
                break;
            case "map.status":
                HandleMapStatus(message.Payload);
                break;
            case "feature.selected":
                HandleFeatureSelected(message);
                break;
            case "bridge.error":
                ShowHostError("Orientation rejected the current map content.");
                break;
        }
    }

    private void HandleMapStatus(JsonElement payload)
    {
        if (!payload.TryGetProperty("status", out var status))
            return;

        switch (status.GetString())
        {
            case "ready":
                HideHostError();
                break;
            case "error":
            case "destroyed":
                ShowHostError("Orientation map rendering is unavailable.");
                break;
        }
    }

    private void HandleFeatureSelected(OrientationHostBridgeMessage message)
    {
        if (viewModel is null
            || !OrientationMapBridgeAdapter.TryGetSelectedFeatureRef(message, out var featureRef))
            return;

        var selected = viewModel.Features.FirstOrDefault(feature => feature.FeatureRef == featureRef);
        viewModel.SelectFeature(selected);
    }

    private async Task SendSceneIfReadyAsync()
    {
        if (disposed || !bridgeReady || viewModel?.IsLoaded != true)
            return;

        var message = OrientationMapBridgeAdapter.CreateSceneReplaceMessage(viewModel.Features);
        var javascriptString = JsonSerializer.Serialize(message);
        try
        {
            await webView.InvokeScript($"window.orientationHostBridge?.receive({javascriptString});");
        }
        catch (Exception error) when (error is InvalidOperationException or ObjectDisposedException)
        {
            ShowHostError("Orientation map host is not ready.");
        }
    }

    private void ShowHostError(string message)
    {
        hostStatus.Text = message;
        AutomationProperties.SetName(hostStatus, message);
        webView.IsVisible = false;
        hostStatus.IsVisible = true;
    }

    private void HideHostError()
    {
        hostStatus.IsVisible = false;
        webView.IsVisible = true;
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        webView.AdapterCreated -= OnAdapterCreated;
        webView.WebMessageReceived -= OnWebMessageReceived;
        webView.NavigationStarted -= OnNavigationStarted;
        webView.NavigationCompleted -= OnNavigationCompleted;
        DataContextChanged -= OnDataContextChanged;
        if (viewModel is not null)
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }
}
