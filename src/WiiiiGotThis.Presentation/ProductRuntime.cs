namespace WiiiiGotThis.Presentation;

public sealed record ProductRuntimeReadiness(bool IsReady, string? FailureMessage = null)
{
    public static ProductRuntimeReadiness Ready { get; } = new(true);

    public static ProductRuntimeReadiness Unavailable(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new ProductRuntimeReadiness(false, message);
    }
}

public interface IVocationProductRuntime
{
    Task<ProductRuntimeReadiness> EnsureReadyAsync(Uri productUri, CancellationToken cancellationToken = default);
}

public interface IOrientationProductRuntime
{
    Task<ProductRuntimeReadiness> EnsureReadyAsync(Uri productUri, CancellationToken cancellationToken = default);
}
