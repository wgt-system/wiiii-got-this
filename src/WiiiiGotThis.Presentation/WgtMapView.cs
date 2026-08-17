using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using Avalonia.Controls;

namespace WiiiiGotThis.Presentation;

public sealed class WgtMapView : Grid, IDisposable
{
    private readonly NativeWebView webView = new();
    private readonly TextBlock hostStatus = new()
    {
        Text = "Orientation map host is unavailable.",
        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        IsVisible = false,
    };
    private readonly IOrientationMapPlatformHost? platformHost = OrientationMapPlatformServices.Host;

    private VocationMapProjectionViewModel? viewModel;
    private string? orientationEmbedPath;
    private bool bridgeReady;
    private bool disposed;

    public WgtMapView()
    {
        Children.Add(webView);
        Children.Add(hostStatus);

        webView.AdapterCreated += OnAdapterCreated;
        webView.WebMessageReceived += OnWebMessageReceived;
        webView.NavigationStarted += OnNavigationStarted;
        webView.NavigationCompleted += OnNavigationCompleted;
        DataContextChanged += OnDataContextChanged;
        if (platformHost is not null)
            platformHost.CurrentPositionChanged += OnCurrentPositionChanged;

        PrepareOrientationHost();
    }

    private void PrepareOrientationHost()
    {
        var configuredPath = Environment.GetEnvironmentVariable("WGT_ORIENTATION_EMBED_PATH");
        if (platformHost is not null)
        {
            if (platformHost.TryResolveEmbedPath(configuredPath, out var platformEmbedPath, out var platformError)
                && !string.IsNullOrWhiteSpace(platformEmbedPath))
            {
                orientationEmbedPath = platformEmbedPath;
                return;
            }

            ShowHostError(platformError ?? "Orientation map host could not resolve its packaged surface.");
            return;
        }

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

        if (OperatingSystem.IsWindows())
        {
            if (!WindowsOrientationWebViewHost.TryConfigure(e.TryGetPlatformHandle(), orientationEmbedPath, out var error))
            {
                ShowHostError(error ?? "Orientation map host could not be configured.");
                return;
            }

            webView.Source = WindowsOrientationWebViewHost.EmbedUri;
            return;
        }

        if (platformHost is null)
        {
            ShowHostError("The Orientation map host is not configured for this platform.");
            return;
        }

        if (!platformHost.TryConfigure(e.TryGetPlatformHandle(), orientationEmbedPath, out var platformError))
            ShowHostError(platformError ?? "Orientation map host could not be configured.");
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
                platformHost?.RequestCurrentPosition();
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

    private void OnCurrentPositionChanged(object? sender, OrientationCurrentPositionChangedEventArgs e)
    {
        _ = SendCurrentPositionIfReadyAsync(e.Position);
    }

    private async Task SendSceneIfReadyAsync()
    {
        if (disposed || !bridgeReady || viewModel?.IsLoaded != true)
            return;

        await SendBridgeMessageAsync(OrientationMapBridgeAdapter.CreateSceneReplaceMessage(viewModel.Features));
    }

    private async Task SendCurrentPositionIfReadyAsync(OrientationCurrentPosition? position)
    {
        if (disposed || !bridgeReady)
            return;

        var message = position is null
            ? OrientationMapBridgeAdapter.CreateCurrentPositionClearMessage()
            : OrientationMapBridgeAdapter.CreateCurrentPositionSetMessage(position);
        await SendBridgeMessageAsync(message);
    }

    private async Task SendBridgeMessageAsync(string message)
    {
        var javascriptString = $"\"{JavaScriptEncoder.Default.Encode(message)}\"";
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
        hostStatus.IsVisible = true;
    }

    private void HideHostError() => hostStatus.IsVisible = false;

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
        if (platformHost is not null)
            platformHost.CurrentPositionChanged -= OnCurrentPositionChanged;
        if (viewModel is not null)
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }
}
