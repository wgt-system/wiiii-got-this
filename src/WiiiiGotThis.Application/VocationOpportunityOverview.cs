using System.Numerics;

namespace WiiiiGotThis.Application;

public sealed record VocationOpportunityOverview(
    string PublicationRef,
    VocationContractTimestamp GeneratedAt,
    IReadOnlyList<VocationOpportunity> Opportunities);

public sealed record VocationContractTimestamp(string RawValue, DateTimeOffset? NormalizedUtc);

public sealed record VocationOpportunity(
    string OpportunityRef,
    string Title,
    VocationCompany Company,
    IReadOnlyList<VocationWorkLocation> WorkLocations,
    BigInteger PostingCount);

public sealed record VocationCompany(string CompanyRef, string Name);

public sealed record VocationWorkLocation(
    string Label,
    string? City,
    string? Region,
    string? CountryCode,
    string Precision);

public interface IVocationOpportunityOverviewSource
{
    ValueTask<VocationOpportunityOverview> GetAsync(CancellationToken cancellationToken = default);
}

public enum VocationOpportunityOverviewSourceFailureKind
{
    Unavailable,
    InvalidContract,
    IncompatibleContract
}

public sealed class VocationOpportunityOverviewSourceException : Exception
{
    public VocationOpportunityOverviewSourceException(
        VocationOpportunityOverviewSourceFailureKind kind,
        string message,
        string? observedContractVersion = null)
        : base(message)
    {
        Kind = kind;
        ObservedContractVersion = observedContractVersion;
    }

    public VocationOpportunityOverviewSourceFailureKind Kind { get; }
    public string? ObservedContractVersion { get; }
}

public enum VocationOpportunityOverviewReadStatus
{
    Loaded,
    Unavailable,
    InvalidContract,
    IncompatibleContract
}

public sealed record VocationOpportunityOverviewReadResult(
    VocationOpportunityOverviewReadStatus Status,
    VocationOpportunityOverview? Snapshot)
{
    public static VocationOpportunityOverviewReadResult Loaded(VocationOpportunityOverview snapshot) => new(VocationOpportunityOverviewReadStatus.Loaded, snapshot);
    public static VocationOpportunityOverviewReadResult Failed(VocationOpportunityOverviewReadStatus status) => new(status, null);
}

public sealed class GetVocationOpportunityOverviewUseCase(IVocationOpportunityOverviewSource source)
{
    public async ValueTask<VocationOpportunityOverviewReadResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return VocationOpportunityOverviewReadResult.Loaded(await source.GetAsync(cancellationToken));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return VocationOpportunityOverviewReadResult.Failed(VocationOpportunityOverviewReadStatus.Unavailable);
        }
        catch (VocationOpportunityOverviewSourceException exception) when (!cancellationToken.IsCancellationRequested)
        {
            var status = exception.Kind switch
            {
                VocationOpportunityOverviewSourceFailureKind.Unavailable => VocationOpportunityOverviewReadStatus.Unavailable,
                VocationOpportunityOverviewSourceFailureKind.InvalidContract => VocationOpportunityOverviewReadStatus.InvalidContract,
                VocationOpportunityOverviewSourceFailureKind.IncompatibleContract => VocationOpportunityOverviewReadStatus.IncompatibleContract,
                _ => VocationOpportunityOverviewReadStatus.Unavailable
            };
            return VocationOpportunityOverviewReadResult.Failed(status);
        }
    }
}
