using System.Net;
using System.Text;
using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;
using WiiiiGotThis.Integrations.Vocation;

namespace WiiiiGotThis.Application.Tests;

public sealed class VocationMapProjectionTests
{
    [Fact]
    public void Metadata_uses_the_frozen_capability_and_contract_version()
    {
        Assert.Equal("vocation.map_projection", VocationIntegrationMetadata.MapProjectionCapability.Value);
        Assert.Equal("Map Projection", VocationIntegrationMetadata.MapProjectionTitle);
        Assert.Equal(new Version(1, 0), VocationIntegrationMetadata.MapProjectionContractVersion);
    }

    [Fact]
    public void Reader_preserves_the_accepted_published_map_data_and_precision()
    {
        var projection = VocationMapProjectionContractReader.Read(CanonicalJson);

        Assert.Equal("publication-1", projection.PublicationRef);
        Assert.Equal("feature-1", projection.Features[0].FeatureRef);
        Assert.Equal("opportunity-1", projection.Features[0].OpportunityRef);
        Assert.Equal("Senior Role", projection.Features[0].Title);
        Assert.Equal("company-1", projection.Features[0].Company.CompanyRef);
        Assert.Equal("Company", projection.Features[0].Company.Name);
        Assert.Equal("Central Office", projection.Features[0].WorkLocation.Label);
        Assert.Equal("exact_address", projection.Features[0].WorkLocation.Precision);
        Assert.Equal(52.52, projection.Features[0].Coordinates.Latitude);
        Assert.Equal(13.405, projection.Features[0].Coordinates.Longitude);
    }

    [Fact]
    public void Reader_accepts_all_published_precision_values()
    {
        foreach (var precision in new[] { "exact_address", "site", "city", "region", "approximate", "unknown" })
        {
            var projection = VocationMapProjectionContractReader.Read(CanonicalJson.Replace("exact_address", precision, StringComparison.Ordinal));
            Assert.Equal(precision, projection.Features[0].WorkLocation.Precision);
        }
    }

    [Fact]
    public void Reader_requires_exact_capability_and_contract_version()
    {
        var capability = Assert.Throws<VocationPublishedContractValidationException>(() =>
            VocationMapProjectionContractReader.Read(CanonicalJson.Replace("vocation.map_projection", "vocation.other", StringComparison.Ordinal)));
        Assert.Equal(VocationContractFailureKind.UnexpectedCapability, capability.Kind);

        var version = Assert.Throws<VocationPublishedContractValidationException>(() =>
            VocationMapProjectionContractReader.Read(CanonicalJson.Replace("\"1.0\"", "\"2.0\"", StringComparison.Ordinal)));
        Assert.Equal(VocationContractFailureKind.UnsupportedContractVersion, version.Kind);
        Assert.Equal("2.0", version.UnsupportedVersion);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{\"capability\":\"vocation.map_projection\",\"contract_version\":\"1.0\",\"publication\":{},\"features\":[]}")]
    [InlineData("{\"capability\":\"vocation.map_projection\",\"contract_version\":\"1.0\",\"publication\":{\"publication_ref\":\"p\",\"generated_at\":\"2026-08-10T12:00:00Z\"},\"features\":[{\"feature_ref\":\"f\",\"opportunity_ref\":\"o\",\"title\":\"T\",\"company\":{\"company_ref\":\"c\",\"name\":\"C\"},\"work_location\":{\"label\":\"L\",\"precision\":\"invalid\"},\"coordinates\":{\"latitude\":0,\"longitude\":0}}]}")]
    [InlineData("{\"capability\":\"vocation.map_projection\",\"contract_version\":\"1.0\",\"publication\":{\"publication_ref\":\"p\",\"generated_at\":\"2026-08-10T12:00:00Z\"},\"features\":[{\"feature_ref\":\"f\",\"opportunity_ref\":\"o\",\"title\":\"T\",\"company\":{\"company_ref\":\"c\",\"name\":\"C\"},\"work_location\":{\"label\":\"L\",\"precision\":\"city\"},\"coordinates\":{\"latitude\":91,\"longitude\":0}}]}")]
    [InlineData("{\"capability\":\"vocation.map_projection\",\"contract_version\":\"1.0\",\"publication\":{\"publication_ref\":\"p\",\"generated_at\":\"2026-08-10T12:00:00Z\"},\"features\":[{\"feature_ref\":\"f\",\"opportunity_ref\":\"o\",\"title\":\"T\",\"company\":{\"company_ref\":\"c\",\"name\":\"C\"},\"work_location\":{\"label\":\"L\",\"precision\":\"city\"},\"coordinates\":{\"latitude\":0,\"longitude\":181}}]}")]
    [InlineData("{\"capability\":\"vocation.map_projection\",\"contract_version\":\"1.0\",\"publication\":{\"publication_ref\":\"p\",\"generated_at\":\"2026-08-10T12:00:00Z\"},\"features\":[{\"feature_ref\":\"f\",\"opportunity_ref\":\"o\",\"title\":\"T\",\"company\":{\"company_ref\":\"c\",\"name\":\"C\"},\"work_location\":{\"label\":\"L\",\"precision\":\"city\"},\"coordinates\":{\"latitude\":\"0\",\"longitude\":0}}]}")]
    public void Reader_rejects_malformed_contracts(string json)
    {
        var exception = Assert.Throws<VocationPublishedContractValidationException>(() => VocationMapProjectionContractReader.Read(json));
        Assert.Equal(VocationContractFailureKind.MalformedContract, exception.Kind);
    }

    [Fact]
    public void Reader_requires_an_rfc3339_timezone()
    {
        var json = CanonicalJson.Replace("2026-08-10T12:00:00Z", "2026-08-10T12:00:00", StringComparison.Ordinal);
        var exception = Assert.Throws<VocationPublishedContractValidationException>(() => VocationMapProjectionContractReader.Read(json));
        Assert.Equal(VocationContractFailureKind.MalformedContract, exception.Kind);
    }
    [Fact]
    public async Task Http_source_uses_map_projection_path()
    {
        var handler = new RecordingHandler(_ => Response(CanonicalJson));
        var source = new VocationHttpMapProjectionSource(new HttpClient(handler), new Uri("http://localhost:9876/root"));

        var projection = await source.GetAsync();

        Assert.Equal("http://localhost:9876/root/published/v1/map-projection", handler.RequestedUri!.ToString());
        Assert.Equal("feature-1", projection.Features.Single().FeatureRef);
    }

    [Theory]
    [InlineData(VocationMapProjectionSourceFailureKind.Unavailable, "transport")]
    [InlineData(VocationMapProjectionSourceFailureKind.Unavailable, "status")]
    [InlineData(VocationMapProjectionSourceFailureKind.InvalidContract, "malformed")]
    [InlineData(VocationMapProjectionSourceFailureKind.IncompatibleContract, "version")]
    public async Task Http_source_maps_provider_failures(VocationMapProjectionSourceFailureKind expected, string caseName)
    {
        var handler = new RecordingHandler(_ => caseName switch
        {
            "transport" => throw new HttpRequestException(),
            "status" => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            "malformed" => Response("not-json"),
            "version" => Response(CanonicalJson.Replace("\"1.0\"", "\"2.0\"", StringComparison.Ordinal)),
            _ => throw new InvalidOperationException()
        });
        var source = new VocationHttpMapProjectionSource(new HttpClient(handler));
        var failure = await Assert.ThrowsAsync<VocationMapProjectionSourceException>(() => source.GetAsync().AsTask());

        Assert.Equal(expected, failure.Kind);
        if (expected == VocationMapProjectionSourceFailureKind.IncompatibleContract)
            Assert.Equal("2.0", failure.ObservedContractVersion);
    }

    [Fact]
    public async Task Application_read_use_case_maps_failures_without_exposing_provider_exceptions()
    {
        var source = new StubMapSource(new VocationMapProjectionSourceException(VocationMapProjectionSourceFailureKind.InvalidContract, "invalid"));
        var result = await new GetVocationMapProjectionUseCase(source).ExecuteAsync();

        Assert.Equal(VocationMapProjectionReadStatus.InvalidContract, result.Status);
        Assert.Null(result.Snapshot);
    }

    [Fact]
    public async Task Adapter_publishes_both_capabilities_and_observes_them_independently()
    {
        var overview = new StubOverviewSource();
        var map = new StubMapSource(Snapshot());
        var adapter = new VocationIntegrationAdapter(overview, map);

        var publication = await adapter.GetPublicationAsync();
        Assert.Equal(
            ["vocation.opportunity_overview", "vocation.map_projection"],
            publication.Capabilities.Select(x => x.Id.Value));

        var overviewFacts = await adapter.ObserveCapabilityAsync(publication.Capabilities[0]);
        var mapFacts = await adapter.ObserveCapabilityAsync(publication.Capabilities[1]);
        Assert.Equal(ContractCompatibility.Compatible, overviewFacts.ContractCompatibility);
        Assert.Equal(ContractCompatibility.Compatible, mapFacts.ContractCompatibility);

        map.Next = new VocationMapProjectionSourceException(VocationMapProjectionSourceFailureKind.InvalidContract, "invalid");
        overviewFacts = await adapter.ObserveCapabilityAsync(publication.Capabilities[0]);
        mapFacts = await adapter.ObserveCapabilityAsync(publication.Capabilities[1]);
        Assert.Equal(ContractCompatibility.Compatible, overviewFacts.ContractCompatibility);
        Assert.Equal(ProviderReachability.Unknown, mapFacts.ProviderReachability);
    }

    [Fact]
    public async Task Adapter_maps_map_provider_unavailable_and_incompatible_states()
    {
        var map = new StubMapSource(new VocationMapProjectionSourceException(VocationMapProjectionSourceFailureKind.Unavailable, "offline"));
        var adapter = new VocationIntegrationAdapter(new StubOverviewSource(), map);
        var publication = await adapter.GetPublicationAsync();
        var mapCapability = publication.Capabilities.Single(x => x.Id == VocationIntegrationMetadata.MapProjectionCapability);

        var unavailable = await adapter.ObserveCapabilityAsync(mapCapability);
        Assert.Equal(ProviderReachability.Unreachable, unavailable.ProviderReachability);
        Assert.Equal(ContractCompatibility.Compatible, unavailable.ContractCompatibility);

        map.Next = new VocationMapProjectionSourceException(VocationMapProjectionSourceFailureKind.IncompatibleContract, "unsupported", "2.0");
        var incompatible = await adapter.ObserveCapabilityAsync(mapCapability);
        Assert.Equal(ContractCompatibility.Incompatible, incompatible.ContractCompatibility);
    }

    private static HttpResponseMessage Response(string json) =>
        new(HttpStatusCode.OK) { Content = new ByteArrayContent(Encoding.UTF8.GetBytes(json)) };

    private static VocationMapProjection Snapshot() => new(
        "publication-1",
        new("2026-08-10T12:00:00Z", DateTimeOffset.Parse("2026-08-10T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture)),
        [new("feature-1", "opportunity-1", "Role", new("company-1", "Company"), new("Berlin", "city"), new(52.52, 13.405))]);

    private const string CanonicalJson = "{\"capability\":\"vocation.map_projection\",\"contract_version\":\"1.0\",\"publication\":{\"publication_ref\":\"publication-1\",\"generated_at\":\"2026-08-10T12:00:00Z\"},\"features\":[{\"feature_ref\":\"feature-1\",\"opportunity_ref\":\"opportunity-1\",\"title\":\"Senior Role\",\"company\":{\"company_ref\":\"company-1\",\"name\":\"Company\"},\"work_location\":{\"label\":\"Central Office\",\"precision\":\"exact_address\"},\"coordinates\":{\"latitude\":52.52,\"longitude\":13.405}}]}";

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public Uri? RequestedUri { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestedUri = request.RequestUri;
            return Task.FromResult(response(request));
        }
    }

    private sealed class StubOverviewSource : IVocationOpportunityOverviewSource
    {
        public ValueTask<VocationOpportunityOverview> GetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new VocationOpportunityOverview("publication", new("2026-08-10T12:00:00Z", DateTimeOffset.UtcNow), []));
        }
    }

    private sealed class StubMapSource(object? next = null) : IVocationMapProjectionSource
    {
        public object? Next { get; set; } = next;
        public ValueTask<VocationMapProjection> GetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Next is Exception exception) throw exception;
            return ValueTask.FromResult((VocationMapProjection)(Next ?? Snapshot()));
        }
    }
}
