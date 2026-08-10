using System.Text.Json;
using WiiiiGotThis.Integrations.Vocation;

namespace WiiiiGotThis.Application.Tests;

public sealed class VocationOpportunityOverviewContractTests
{
    [Fact]
    public void Vocation_metadata_is_stable_for_the_published_capability()
    {
        Assert.Equal("vocation", VocationIntegrationMetadata.ServiceId.Value);
        Assert.Equal("Vocation", VocationIntegrationMetadata.ServiceDisplayName);
        Assert.Equal("vocation.opportunity_overview", VocationIntegrationMetadata.OpportunityOverviewCapability.Value);
        Assert.Equal("Opportunity Overview", VocationIntegrationMetadata.OpportunityOverviewTitle);
        Assert.Equal(new Version(1, 0), VocationIntegrationMetadata.OpportunityOverviewContractVersion);
    }

    [Fact]
    public void Canonical_artifact_preserves_publication_opportunity_company_and_location_data()
    {
        var overview = VocationOpportunityOverviewContractReader.Read(CanonicalJson);
        var opportunity = Assert.Single(overview.Opportunities);
        var location = Assert.Single(opportunity.WorkLocations);
        Assert.Equal("publication-α", overview.PublicationRef);
        Assert.Equal(DateTimeOffset.Parse("2026-08-10T12:34:56Z", System.Globalization.CultureInfo.InvariantCulture), overview.GeneratedAt);
        Assert.Equal("opportunity/α", opportunity.OpportunityRef);
        Assert.Equal("Senior Role", opportunity.Title);
        Assert.Equal("company/α", opportunity.Company.CompanyRef);
        Assert.Equal("Company α", opportunity.Company.Name);
        Assert.Equal("Central Office", location.Label);
        Assert.Equal("Berlin", location.City);
        Assert.Equal("BE", location.Region);
        Assert.Equal("DE", location.CountryCode);
        Assert.Equal("exact_address", location.Precision);
        Assert.Equal(2, opportunity.PostingCount);
    }

    [Fact]
    public void Empty_opportunities_are_valid()
    {
        var overview = VocationOpportunityOverviewContractReader.Read(EmptyOpportunitiesJson);
        Assert.Empty(overview.Opportunities);
    }

    [Fact]
    public void Multiple_opportunities_and_locations_are_preserved_in_order()
    {
        var locations = $"[{LocationJson("One", "city", "region", null, "site")},{LocationJson("Two", null, null, "US", "city")}]";
        var opportunities = $"[{OpportunityJson("one", 0, locations, "First")},{OpportunityJson("two", 4, "[]", "Second")}]";
        var json = $"{{\"capability\":\"vocation.opportunity_overview\",\"contract_version\":\"1.0\",\"publication\":{{\"publication_ref\":\"pub\",\"generated_at\":\"2026-08-10T12:34:56+02:00\"}},\"opportunities\":{opportunities}}}";
        var overview = VocationOpportunityOverviewContractReader.Read(json);
        Assert.Equal(["one", "two"], overview.Opportunities.Select(x => x.OpportunityRef));
        Assert.Equal(["One", "Two"], overview.Opportunities[0].WorkLocations.Select(x => x.Label));
        Assert.Null(overview.Opportunities[0].WorkLocations[1].City);
        Assert.Equal(4, overview.Opportunities[1].PostingCount);
    }

    [Fact]
    public void Nullable_location_fields_and_each_precision_value_are_accepted()
    {
        foreach (var precision in new[] { "exact_address", "site", "city", "region", "approximate", "unknown" })
        {
            var json = CanonicalJson.Replace("\"precision\":\"exact_address\"", $"\"precision\":\"{precision}\"", StringComparison.Ordinal)
                .Replace("\"city\":\"Berlin\",\"region\":\"BE\",\"country_code\":\"DE\"", "\"city\":null,\"region\":null,\"country_code\":null", StringComparison.Ordinal);
            var location = Assert.Single(Assert.Single(VocationOpportunityOverviewContractReader.Read(json).Opportunities).WorkLocations);
            Assert.Equal(precision, location.Precision);
            Assert.Null(location.City); Assert.Null(location.Region); Assert.Null(location.CountryCode);
        }
    }

    [Fact]
    public void Exact_capability_and_version_are_required()
    {
        Assert.Equal("vocation.opportunity_overview", JsonDocument.Parse(CanonicalJson).RootElement.GetProperty("capability").GetString());
        Assert.Equal("1.0", JsonDocument.Parse(CanonicalJson).RootElement.GetProperty("contract_version").GetString());
        Assert.Throws<VocationPublishedContractValidationException>(() => VocationOpportunityOverviewContractReader.Read(CanonicalJson.Replace("vocation.opportunity_overview", "other.capability", StringComparison.Ordinal)));
        var exception = Assert.Throws<VocationPublishedContractValidationException>(() => VocationOpportunityOverviewContractReader.Read(CanonicalJson.Replace("\"1.0\"", "\"2.0\"", StringComparison.Ordinal)));
        Assert.Equal(VocationContractFailureKind.UnsupportedContractVersion, exception.Kind);
        Assert.Equal("2.0", exception.UnsupportedVersion);
    }

    [Fact]
    public void Wrong_capability_is_distinct_from_malformed_contract()
    {
        var exception = Assert.Throws<VocationPublishedContractValidationException>(() => VocationOpportunityOverviewContractReader.Read(CanonicalJson.Replace("vocation.opportunity_overview", "vocation.other", StringComparison.Ordinal)));
        Assert.Equal(VocationContractFailureKind.UnexpectedCapability, exception.Kind);
    }

    [Fact]
    public void Missing_or_unexpected_root_properties_are_rejected()
    {
        AssertMalformed(CanonicalJson.Replace(",\"opportunities\":[", ",", StringComparison.Ordinal));
        AssertMalformed(CanonicalJson.Replace("\"capability\":", "\"unexpected\":\"x\",\"capability\":", StringComparison.Ordinal));
    }

    [Fact]
    public void Malformed_json_and_malformed_nested_shapes_are_rejected_as_malformed_contracts()
    {
        AssertMalformed("{not-json");
        AssertMalformed(CanonicalJson.Replace("\"publication_ref\":\"publication-α\"", "\"publication_ref\":null", StringComparison.Ordinal));
        AssertMalformed(CanonicalJson.Replace("\"opportunity_ref\":\"opportunity/α\"", "\"opportunity_ref\":null", StringComparison.Ordinal));
        AssertMalformed(CanonicalJson.Replace("\"company_ref\":\"company/α\"", "\"company_ref\":null", StringComparison.Ordinal));
        AssertMalformed(CanonicalJson.Replace("\"label\":\"Central Office\"", "\"label\":null", StringComparison.Ordinal));
        AssertMalformed(CanonicalJson.Replace("\"company\":{", "\"company\":{\"unexpected\":true,", StringComparison.Ordinal));
    }

    [Fact]
    public void Invalid_values_are_rejected_without_reinterpreting_provider_data()
    {
        AssertMalformed(CanonicalJson.Replace("publication-α", "", StringComparison.Ordinal));
        AssertMalformed(CanonicalJson.Replace("publication-α", new string('x', 201), StringComparison.Ordinal));
        AssertMalformed(CanonicalJson.Replace("2026-08-10T12:34:56Z", "not-a-date", StringComparison.Ordinal));
        AssertMalformed(CanonicalJson.Replace("\"DE\"", "\"de\"", StringComparison.Ordinal));
        AssertMalformed(CanonicalJson.Replace("exact_address", "invalid", StringComparison.Ordinal));
        AssertMalformed(CanonicalJson.Replace("\"posting_count\":2", "\"posting_count\":-1", StringComparison.Ordinal));
        AssertMalformed(CanonicalJson.Replace("\"posting_count\":2", "\"posting_count\":1.5", StringComparison.Ordinal));
    }

    [Fact]
    public void Unexpected_properties_at_each_nested_boundary_are_rejected()
    {
        AssertMalformed(CanonicalJson.Replace("\"generated_at\":", "\"extra\":true,\"generated_at\":", StringComparison.Ordinal));
        AssertMalformed(CanonicalJson.Replace("\"title\":", "\"extra\":true,\"title\":", StringComparison.Ordinal));
        AssertMalformed(CanonicalJson.Replace("\"name\":", "\"extra\":true,\"name\":", StringComparison.Ordinal));
        AssertMalformed(CanonicalJson.Replace("\"precision\":", "\"extra\":true,\"precision\":", StringComparison.Ordinal));
    }

    private static void AssertMalformed(string json)
    {
        var exception = Assert.Throws<VocationPublishedContractValidationException>(() => VocationOpportunityOverviewContractReader.Read(json));
        Assert.Equal(VocationContractFailureKind.MalformedContract, exception.Kind);
    }

    private static string OpportunityJson(string reference, int postingCount, string locations, string title) =>
        $"{{\"opportunity_ref\":\"{reference}\",\"title\":\"{title}\",\"company\":{{\"company_ref\":\"company-{reference}\",\"name\":\"Company\"}},\"work_locations\":{locations},\"posting_count\":{postingCount}}}";

    private static string LocationJson(string label, string? city, string? region, string? countryCode, string precision) =>
        $"{{\"label\":\"{label}\",\"city\":{JsonSerializer.Serialize(city)},\"region\":{JsonSerializer.Serialize(region)},\"country_code\":{JsonSerializer.Serialize(countryCode)},\"precision\":\"{precision}\"}}";

    private const string CanonicalJson = "{\"capability\":\"vocation.opportunity_overview\",\"contract_version\":\"1.0\",\"publication\":{\"publication_ref\":\"publication-α\",\"generated_at\":\"2026-08-10T12:34:56Z\"},\"opportunities\":[{\"opportunity_ref\":\"opportunity/α\",\"title\":\"Senior Role\",\"company\":{\"company_ref\":\"company/α\",\"name\":\"Company α\"},\"work_locations\":[{\"label\":\"Central Office\",\"city\":\"Berlin\",\"region\":\"BE\",\"country_code\":\"DE\",\"precision\":\"exact_address\"}],\"posting_count\":2}]}";
    private const string EmptyOpportunitiesJson = "{\"capability\":\"vocation.opportunity_overview\",\"contract_version\":\"1.0\",\"publication\":{\"publication_ref\":\"publication\",\"generated_at\":\"2026-08-10T12:34:56Z\"},\"opportunities\":[]}";
}
