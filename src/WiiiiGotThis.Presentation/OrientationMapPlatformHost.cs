using Avalonia.Platform;

namespace WiiiiGotThis.Presentation;

public sealed record OrientationCurrentPosition(
    double Longitude,
    double Latitude,
    double AccuracyMeters,
    DateTimeOffset ObservedAt);

public sealed class OrientationCurrentPositionChangedEventArgs(OrientationCurrentPosition? position) : EventArgs
{
    public OrientationCurrentPosition? Position { get; } = position;
}

public interface IOrientationMapPlatformHost
{
    event EventHandler<OrientationCurrentPositionChangedEventArgs>? CurrentPositionChanged;

    bool TryResolveEmbedPath(string? configuredPath, out string? embedPath, out string? error);

    bool TryConfigure(IPlatformHandle? platformHandle, string embedPath, out string? error);

    void RequestCurrentPosition();
}

public static class OrientationMapPlatformServices
{
    private static IOrientationMapPlatformHost? host;

    public static IOrientationMapPlatformHost? Host
    {
        get => host;
        set => host = value;
    }
}
