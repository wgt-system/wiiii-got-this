#if WGT_IOS
using Avalonia.Platform;
using CoreLocation;
using Foundation;
using ObjCRuntime;
using WebKit;
using WiiiiGotThis.Presentation;

namespace WiiiiGotThis.iOS;

internal sealed class IosOrientationMapPlatformHost : IOrientationMapPlatformHost, IDisposable
{
    private readonly CLLocationManager locationManager = new();
    private readonly LocationDelegate locationDelegate;
    private bool disposed;

    public IosOrientationMapPlatformHost()
    {
        locationDelegate = new LocationDelegate(this);
        locationManager.Delegate = locationDelegate;
        locationManager.DesiredAccuracy = CLLocation.AccuracyNearestTenMeters;
    }

    public event EventHandler<OrientationCurrentPositionChangedEventArgs>? CurrentPositionChanged;

    public bool TryResolveEmbedPath(string? configuredPath, out string? embedPath, out string? failureMessage)
    {
        embedPath = null;
        failureMessage = null;

        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            try
            {
                var fullPath = Path.GetFullPath(configuredPath.Trim());
                if (!File.Exists(fullPath))
                {
                    failureMessage = $"Orientation map host was not found at {fullPath}.";
                    return false;
                }

                embedPath = fullPath;
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                failureMessage = "Orientation map host path is invalid.";
                return false;
            }
        }

        var bundledPath = NSBundle.MainBundle.PathForResource("embed", "html", "orientation-map");
        if (string.IsNullOrWhiteSpace(bundledPath) || !File.Exists(bundledPath))
        {
            failureMessage = "The packaged Orientation map surface is missing from the iOS application bundle.";
            return false;
        }

        embedPath = bundledPath;
        return true;
    }

    public bool TryConfigure(IPlatformHandle? platformHandle, string embedPath, out string? failureMessage)
    {
        failureMessage = null;
        if (platformHandle is not IAppleWKWebViewPlatformHandle appleHandle || appleHandle.WKWebView == IntPtr.Zero)
        {
            failureMessage = "The iOS Orientation map host requires an Avalonia WKWebView platform handle.";
            return false;
        }

        var webView = Runtime.GetNSObject<WKWebView>(appleHandle.WKWebView);
        if (webView is null)
        {
            failureMessage = "The native WKWebView instance could not be resolved.";
            return false;
        }

        var directory = Path.GetDirectoryName(embedPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            failureMessage = "The packaged Orientation map directory could not be resolved.";
            return false;
        }

        using var embedUrl = NSUrl.FromFilename(embedPath);
        using var directoryUrl = NSUrl.FromFilename(directory);
        _ = webView.LoadFileUrl(embedUrl, directoryUrl);
        return true;
    }

    public void RequestCurrentPosition()
    {
        if (disposed)
            return;

        switch (locationManager.AuthorizationStatus)
        {
            case CLAuthorizationStatus.NotDetermined:
                locationManager.RequestWhenInUseAuthorization();
                break;
            case CLAuthorizationStatus.AuthorizedAlways:
            case CLAuthorizationStatus.AuthorizedWhenInUse:
                locationManager.RequestLocation();
                break;
            case CLAuthorizationStatus.Denied:
            case CLAuthorizationStatus.Restricted:
            default:
                PublishCurrentPosition(null);
                break;
        }
    }

    private void AuthorizationChanged()
    {
        if (disposed)
            return;

        switch (locationManager.AuthorizationStatus)
        {
            case CLAuthorizationStatus.AuthorizedAlways:
            case CLAuthorizationStatus.AuthorizedWhenInUse:
                locationManager.RequestLocation();
                break;
            case CLAuthorizationStatus.Denied:
            case CLAuthorizationStatus.Restricted:
                PublishCurrentPosition(null);
                break;
        }
    }

    private void LocationsUpdated(CLLocation[] locations)
    {
        if (disposed)
            return;

        var location = locations.LastOrDefault(IsUsableLocation);
        if (location is null)
        {
            PublishCurrentPosition(null);
            return;
        }

        DateTimeOffset observedAt;
        try
        {
            observedAt = DateTimeOffset.UnixEpoch.AddSeconds(location.Timestamp.SecondsSince1970);
        }
        catch (ArgumentOutOfRangeException)
        {
            observedAt = DateTimeOffset.UtcNow;
        }

        PublishCurrentPosition(new OrientationCurrentPosition(
            location.Coordinate.Longitude,
            location.Coordinate.Latitude,
            location.HorizontalAccuracy,
            observedAt));
    }

    private static bool IsUsableLocation(CLLocation location)
    {
        var longitude = location.Coordinate.Longitude;
        var latitude = location.Coordinate.Latitude;
        return double.IsFinite(longitude)
            && longitude is >= -180 and <= 180
            && double.IsFinite(latitude)
            && latitude is >= -90 and <= 90
            && double.IsFinite(location.HorizontalAccuracy)
            && location.HorizontalAccuracy >= 0;
    }

    private void PublishCurrentPosition(OrientationCurrentPosition? position) =>
        CurrentPositionChanged?.Invoke(this, new OrientationCurrentPositionChangedEventArgs(position));

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        locationManager.Dispose();
        locationDelegate.Dispose();
    }

    private sealed class LocationDelegate(IosOrientationMapPlatformHost owner) : CLLocationManagerDelegate
    {
        public override void DidChangeAuthorization(CLLocationManager manager) => owner.AuthorizationChanged();

        public override void LocationsUpdated(CLLocationManager manager, CLLocation[] locations) => owner.LocationsUpdated(locations);

        public override void Failed(CLLocationManager manager, NSError error) => owner.PublishCurrentPosition(null);
    }
}
#endif
