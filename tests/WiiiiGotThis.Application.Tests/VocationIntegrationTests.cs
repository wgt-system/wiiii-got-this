using System.Net;
using System.Net.Http;
using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;
using WiiiiGotThis.Integrations.Vocation;

namespace WiiiiGotThis.Application.Tests;

public sealed class VocationIntegrationTests
{
    [Fact]
    public async Task Http_source_uses_configured_base_uri_and_published_path()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(ValidJson))
        });
        var source = new VocationHttpOpportunityOverviewSource(new HttpClient(handler), new Uri("http://localhost:9876/root"));

        var snapshot = await source.GetAsync();

        Assert.Equal("http://localhost:9876/root/published/v1/opportunity-overview", handler.RequestedUri!.ToString());
        Assert.Equal("publication-1", snapshot.PublicationRef);
        Assert.Single(snapshot.Opportunities);
    }

    [Fact]
    public async Task Http_source_preserves_empty_valid_snapshot()
    {
        var source = Source(ValidJson.Replace("[{\"opportunity_ref\":\"opportunity-1\",\"title\":\"Role\",\"company\":{\"company_ref\":\"company-1\",\"name\":\"Company\"},\"work_locations\":[],\"posting_count\":2}]", "[]", StringComparison.Ordinal));

        var snapshot = await source.GetAsync();

        Assert.Empty(snapshot.Opportunities);
    }

    [Theory]
    [InlineData(VocationOpportunityOverviewSourceFailureKind.Unavailable, "transport")]
    [InlineData(VocationOpportunityOverviewSourceFailureKind.Unavailable, "status")]
    [InlineData(VocationOpportunityOverviewSourceFailureKind.InvalidContract, "malformed")]
    [InlineData(VocationOpportunityOverviewSourceFailureKind.InvalidContract, "capability")]
    [InlineData(VocationOpportunityOverviewSourceFailureKind.IncompatibleContract, "version")]
    public async Task Http_source_maps_provider_failures_to_bounded_categories(VocationOpportunityOverviewSourceFailureKind expected, string caseName)
    {
        var handler = new RecordingHandler(_ => caseName switch
        {
            "transport" => throw new HttpRequestException(),
            "status" => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            "malformed" => Response("not json"),
            "capability" => Response(ValidJson.Replace("vocation.opportunity_overview", "vocation.other", StringComparison.Ordinal)),
            "version" => Response(ValidJson.Replace("\"contract_version\":\"1.0\"", "\"contract_version\":\"99.0\"", StringComparison.Ordinal)),
            _ => throw new InvalidOperationException()
        });
        var source = new VocationHttpOpportunityOverviewSource(new HttpClient(handler));

        var failure = await Assert.ThrowsAsync<VocationOpportunityOverviewSourceException>(() => source.GetAsync().AsTask());

        Assert.Equal(expected, failure.Kind);
        if (expected == VocationOpportunityOverviewSourceFailureKind.IncompatibleContract)
            Assert.Equal("99.0", failure.ObservedContractVersion);
    }

    [Fact]
    public async Task Caller_cancellation_propagates()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var source = new VocationHttpOpportunityOverviewSource(new HttpClient(new RecordingHandler(_ => Response(ValidJson))));

        await Assert.ThrowsAsync<OperationCanceledException>(() => source.GetAsync(cancellation.Token).AsTask());
    }

    [Fact]
    public async Task Application_read_use_case_returns_bounded_result_without_provider_exception()
    {
        var source = new StubSource(new VocationOpportunityOverviewSourceException(VocationOpportunityOverviewSourceFailureKind.InvalidContract, "invalid"));

        var result = await new GetVocationOpportunityOverviewUseCase(source).ExecuteAsync();

        Assert.Equal(VocationOpportunityOverviewReadStatus.InvalidContract, result.Status);
        Assert.Null(result.Snapshot);
    }

    [Fact]
    public async Task Vocation_adapter_publishes_one_capability_and_resolves_available()
    {
        var adapter = new VocationIntegrationAdapter(new StubSource(snapshot: Snapshot()), new FixedTimeProvider(new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero)));

        var publication = await adapter.GetPublicationAsync();
        var integration = new ServiceIntegration(adapter.ServiceId); integration.EnableGlobally();
        var resolution = CapabilityResolver.Resolve(integration, DeviceIdentity.New(), new(adapter.ServiceId, publication.Capabilities[0].Id), await adapter.ObserveCapabilityAsync(publication.Capabilities[0]));

        Assert.Equal(new ServiceIdentity("vocation"), publication.ServiceId);
        Assert.Equal("Vocation", publication.DisplayName);
        Assert.Single(publication.Capabilities);
        Assert.Equal("vocation.opportunity_overview", publication.Capabilities[0].Id.Value);
        Assert.Equal(new Version(1, 0), publication.Capabilities[0].ContractVersion);
        Assert.Null(resolution.Availability.Reason);
    }

    [Fact]
    public async Task Unsupported_version_publishes_incompatible_capability_state()
    {
        var source = new StubSource(new VocationOpportunityOverviewSourceException(VocationOpportunityOverviewSourceFailureKind.IncompatibleContract, "unsupported", "99.0"));
        var adapter = new VocationIntegrationAdapter(source);

        var publication = await adapter.GetPublicationAsync();
        var integration = new ServiceIntegration(adapter.ServiceId); integration.EnableGlobally();
        var resolution = CapabilityResolver.Resolve(integration, DeviceIdentity.New(), new(adapter.ServiceId, publication.Capabilities[0].Id), await adapter.ObserveCapabilityAsync(publication.Capabilities[0]));

        Assert.Equal(new Version(99, 0), publication.Capabilities[0].ContractVersion);
        Assert.Equal(AvailabilityReason.Incompatible, resolution.Availability.Reason);
    }

    private static VocationHttpOpportunityOverviewSource Source(string json) => new(new HttpClient(new RecordingHandler(_ => Response(json))));
    private static HttpResponseMessage Response(string content) => new(HttpStatusCode.OK) { Content = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(content)) };
    private static VocationOpportunityOverview Snapshot() => new("publication-1", new("2026-08-10T12:00:00Z", DateTimeOffset.UtcNow), []);

    private const string ValidJson = "{\"capability\":\"vocation.opportunity_overview\",\"contract_version\":\"1.0\",\"publication\":{\"publication_ref\":\"publication-1\",\"generated_at\":\"2026-08-10T12:00:00Z\"},\"opportunities\":[{\"opportunity_ref\":\"opportunity-1\",\"title\":\"Role\",\"company\":{\"company_ref\":\"company-1\",\"name\":\"Company\"},\"work_locations\":[],\"posting_count\":2}]}";

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public Uri? RequestedUri { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestedUri = request.RequestUri;
            return Task.FromResult(response(request));
        }
    }

    private sealed class StubSource(Exception? failure = null, VocationOpportunityOverview? snapshot = null) : IVocationOpportunityOverviewSource
    {
        public ValueTask<VocationOpportunityOverview> GetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (failure is not null) throw failure;
            return ValueTask.FromResult(snapshot ?? Snapshot());
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
