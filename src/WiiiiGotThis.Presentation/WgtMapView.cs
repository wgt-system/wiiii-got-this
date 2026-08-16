using System.ComponentModel;
using System.Text.Json;
using Avalonia.Controls;

namespace WiiiiGotThis.Presentation;

public sealed class WgtMapView : Grid, IDisposable
{
    private const string BridgeContract = "orientation.host-bridge";
    private const string BridgeVersion = "1.0";
    private const string VocationMapSourceRef = "vocation.map_projection";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly NativeWebView webView = new();
    private readonly TextBlock hostStatus = new()
    {
        Text = "Orientation map host is unavailable.",
        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        IsVisible = false,
    };

    private VocationMapProjectionViewModel? viewModel;
    private bool bridgeReady;
    private bool disposed;

    public WgtMapView()
    {
        Children.Add(webView);
        Children.Add(hostStatus);

        webView.WebMessageReceived += OnWebMessageReceived;
        webView.NavigationCompleted += OnNavigationCompleted;
        DataContextChanged += OnDataContextChanged;

        NavigateToOrientationHost();
    }

    private void NavigateToOrientationHost()
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
            webView.Source = new Uri(Path.GetFullPath(embedPath));
        }
        catch (Exception error) when (error is ArgumentException or UriFormatException or NotSupportedException)
        {
            ShowHostError("Orientation map host path is invalid.");
        }
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
        var body = e.Body;
        if (string.IsNullOrWhiteSpace(body))
        {
            ShowHostError("Orientation returned an empty host message.");
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (!MatchesBridge(root))
                return;

            var type = root.GetProperty("type").GetString();
            switch (type)
            {
                case "bridge.ready":
                    bridgeReady = true;
                    HideHostError();
                    _ = SendSceneIfReadyAsync();
                    break;
                case "map.status":
                    HandleMapStatus(root.GetProperty("payload"));
                    break;
                case "feature.selected":
                    HandleFeatureSelected(root.GetProperty("payload"));
                    break;
                case "bridge.error":
                    ShowHostError("Orientation rejected the current map content.");
                    break;
            }
        }
        catch (JsonException)
        {
            ShowHostError("Orientation returned an invalid host message.");
        }
    }

    private static bool MatchesBridge(JsonElement root)
    {
        return root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("contract", out var contract)
            && contract.GetString() == BridgeContract
            && root.TryGetProperty("version", out var version)
            && version.GetString() == BridgeVersion
            && root.TryGetProperty("type", out var type)
            && type.ValueKind == JsonValueKind.String
            && root.TryGetProperty("payload", out var payload)
            && payload.ValueKind == JsonValueKind.Object;
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

    private void HandleFeatureSelected(JsonElement payload)
    {
        if (viewModel is null
            || !payload.TryGetProperty("featureRef", out var featureRefElement)
            || !payload.TryGetProperty("sourceRef", out var sourceRefElement)
            || sourceRefElement.GetString() != VocationMapSourceRef)
            return;

        var featureRef = featureRefElement.GetString();
        var selected = viewModel.Features.FirstOrDefault(feature => feature.FeatureRef == featureRef);
        viewModel.SelectFeature(selected);
    }

    private async Task SendSceneIfReadyAsync()
    {
        if (disposed || !bridgeReady || viewModel?.IsLoaded != true)
            return;

        var message = JsonSerializer.Serialize(new
        {
            contract = BridgeContract,
            version = BridgeVersion,
            type = "scene.replace",
            payload = new
            {
                features = viewModel.Features.Select(ToOrientationFeature).ToArray(),
                viewport = new
                {
                    kind = "automatic",
                    padding = 48,
                    maxZoom = 15,
                },
            },
        }, JsonOptions);

        var javascriptString = JsonSerializer.Serialize(message, JsonOptions);
        try
        {
            await webView.InvokeScript($"window.orientationHostBridge?.receive({javascriptString});");
        }
        catch (Exception error) when (error is InvalidOperationException or ObjectDisposedException)
        {
            ShowHostError("Orientation map host is not ready.");
        }
    }

    private static object ToOrientationFeature(VocationMapFeaturePresentationViewModel feature)
    {
        return new
        {
            @ref = feature.FeatureRef,
            sourceRef = VocationMapSourceRef,
            coordinate = new
            {
                longitude = feature.Longitude,
                latitude = feature.Latitude,
            },
            title = feature.Title,
            subtitle = $"{feature.CompanyName} · {feature.WorkLocationLabel}",
            information = new[]
            {
                new
                {
                    title = "Vocation",
                    rows = new[]
                    {
                        new { label = "Company", value = feature.CompanyName },
                        new { label = "Location", value = feature.WorkLocationLabel },
                        new { label = "Precision", value = feature.WorkLocationPrecision },
                    },
                },
            },
        };
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
        webView.WebMessageReceived -= OnWebMessageReceived;
        webView.NavigationCompleted -= OnNavigationCompleted;
        DataContextChanged -= OnDataContextChanged;
        if (viewModel is not null)
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }
}
