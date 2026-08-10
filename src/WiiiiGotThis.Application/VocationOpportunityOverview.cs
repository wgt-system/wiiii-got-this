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
