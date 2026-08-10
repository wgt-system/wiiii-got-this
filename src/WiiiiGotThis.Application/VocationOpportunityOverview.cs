namespace WiiiiGotThis.Application;

public sealed record VocationOpportunityOverview(
    string PublicationRef,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<VocationOpportunity> Opportunities);

public sealed record VocationOpportunity(
    string OpportunityRef,
    string Title,
    VocationCompany Company,
    IReadOnlyList<VocationWorkLocation> WorkLocations,
    long PostingCount);

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
