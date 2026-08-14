namespace WiiiiGotThis.Application;

public sealed record VocationMapProjection(
    string PublicationRef,
    VocationContractTimestamp GeneratedAt,
    IReadOnlyList<VocationMapFeature> Features);

public sealed record VocationMapFeature(
    string FeatureRef,
    string OpportunityRef,
    string Title,
    VocationMapCompany Company,
    VocationMapWorkLocation WorkLocation,
    VocationMapCoordinates Coordinates);

public sealed record VocationMapCompany(string CompanyRef, string Name);

public sealed record VocationMapWorkLocation(string Label, string Precision);

public sealed record VocationMapCoordinates(double Latitude, double Longitude);

public interface IVocationMapProjectionSource
{
    ValueTask<VocationMapProjection> GetAsync(CancellationToken cancellationToken = default);
}

public enum VocationMapProjectionSourceFailureKind
{
    Unavailable,
    InvalidContract,
    IncompatibleContract
}

public sealed class VocationMapProjectionSourceException : Exception
{
    public VocationMapProjectionSourceException(
        VocationMapProjectionSourceFailureKind kind,
        string message,
        string? observedContractVersion = null)
        : base(message)
    {
        Kind = kind;
        ObservedContractVersion = observedContractVersion;
    }

    public VocationMapProjectionSourceFailureKind Kind { get; }
    public string? ObservedContractVersion { get; }
}

public enum VocationMapProjectionReadStatus
{
    Loaded,
    Unavailable,
    InvalidContract,
    IncompatibleContract
}

public sealed record VocationMapProjectionReadResult(
    VocationMapProjectionReadStatus Status,
    VocationMapProjection? Snapshot)
{
    public static VocationMapProjectionReadResult Loaded(VocationMapProjection snapshot) => new(VocationMapProjectionReadStatus.Loaded, snapshot);
    public static VocationMapProjectionReadResult Failed(VocationMapProjectionReadStatus status) => new(status, null);
}

public sealed class GetVocationMapProjectionUseCase(IVocationMapProjectionSource source)
{
    public async ValueTask<VocationMapProjectionReadResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return VocationMapProjectionReadResult.Loaded(await source.GetAsync(cancellationToken));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return VocationMapProjectionReadResult.Failed(VocationMapProjectionReadStatus.Unavailable);
        }
        catch (VocationMapProjectionSourceException exception) when (!cancellationToken.IsCancellationRequested)
        {
            var status = exception.Kind switch
            {
                VocationMapProjectionSourceFailureKind.Unavailable => VocationMapProjectionReadStatus.Unavailable,
                VocationMapProjectionSourceFailureKind.InvalidContract => VocationMapProjectionReadStatus.InvalidContract,
                VocationMapProjectionSourceFailureKind.IncompatibleContract => VocationMapProjectionReadStatus.IncompatibleContract,
                _ => VocationMapProjectionReadStatus.Unavailable
            };
            return VocationMapProjectionReadResult.Failed(status);
        }
    }
}
