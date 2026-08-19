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

/// <summary>
/// Optional host-facing startup telemetry. It carries no provider domain data and does not turn
/// provider runtimes into a universal protocol; concrete desktop supervisors may expose concise
/// readiness stages so a real first start never looks like an unexplained infinite spinner.
/// </summary>
public interface IProductRuntimeStatusSource
{
    event Action<string>? StageChanged;
}

public interface IVocationProductRuntime
{
    Task<ProductRuntimeReadiness> EnsureReadyAsync(Uri productUri, CancellationToken cancellationToken = default);
}

public interface IOrientationProductRuntime
{
    Task<ProductRuntimeReadiness> EnsureReadyAsync(Uri productUri, CancellationToken cancellationToken = default);
}
