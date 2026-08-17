using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Application.Tests;

public sealed class AtlasProjectionTests
{
    [Fact]
    public void Build_creates_core_service_and_capability_hierarchy_from_runtime_state()
    {
        var vocation = new ServiceIdentity("vocation");
        var opportunityOverview = new CapabilityIdentity("vocation.opportunity_overview");
        var integration = new ServiceIntegrationListItem(
            vocation,
            "Vocation",
            true,
            null,
            true,
            true,
            true,
            IntegrationRefreshStatus.Refreshed,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
        var capability = new CapabilityCatalogEntry(
            vocation,
            "Vocation",
            opportunityOverview,
            "Opportunity Overview",
            new Version(1, 0),
            new CapabilityResolutionResult(opportunityOverview, Enablement.Enabled, Availability.Available));

        var projection = new BuildAtlasProjectionUseCase().Build([integration], [capability]);

        Assert.Collection(
            projection.Nodes,
            core =>
            {
                Assert.Equal(BuildAtlasProjectionUseCase.CoreNodeId, core.NodeId);
                Assert.Equal(AtlasNodeKind.Core, core.Kind);
            },
            service =>
            {
                Assert.Equal("service:vocation", service.NodeId);
                Assert.Equal(AtlasNodeKind.Service, service.Kind);
                Assert.Equal("Vocation", service.Title);
                Assert.True(service.IsAvailable);
            },
            item =>
            {
                Assert.Equal("capability:vocation:vocation.opportunity_overview", item.NodeId);
                Assert.Equal(AtlasNodeKind.Capability, item.Kind);
                Assert.True(item.IsAvailable);
            });
        Assert.Contains(projection.Connections, edge => edge.Kind == AtlasConnectionKind.Composition && edge.SourceNodeId == "wgt.core" && edge.TargetNodeId == "service:vocation");
        Assert.Contains(projection.Connections, edge => edge.Kind == AtlasConnectionKind.CapabilityOwnership && edge.SourceNodeId == "service:vocation" && edge.TargetNodeId == "capability:vocation:vocation.opportunity_overview");
    }

    [Fact]
    public void Build_orders_services_stably_by_product_title()
    {
        var orientation = Integration("orientation", "Orientation");
        var vocation = Integration("vocation", "Vocation");
        var illumination = Integration("illumination", "Illumination");

        var projection = new BuildAtlasProjectionUseCase().Build([vocation, orientation, illumination], []);

        Assert.Equal(
            ["wgt.core", "service:illumination", "service:orientation", "service:vocation"],
            projection.Nodes.Select(node => node.NodeId));
    }

    [Fact]
    public void Build_hides_developer_reference_integration_from_normal_Atlas()
    {
        var reference = new ServiceIdentity("reference");
        var integration = new ServiceIntegrationListItem(
            reference,
            "Reference",
            true,
            null,
            true,
            true,
            true,
            IntegrationRefreshStatus.Refreshed,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        var projection = new BuildAtlasProjectionUseCase().Build([integration], []);

        Assert.Single(projection.Nodes);
        Assert.Equal(AtlasNodeKind.Core, projection.Nodes[0].Kind);
        Assert.Empty(projection.Connections);
    }

    [Fact]
    public void Build_preserves_unavailable_capability_reason_without_inventing_product_semantics()
    {
        var service = new ServiceIdentity("sample");
        var capabilityId = new CapabilityIdentity("sample.capability");
        var integration = new ServiceIntegrationListItem(
            service,
            "Sample",
            true,
            null,
            true,
            true,
            true,
            IntegrationRefreshStatus.Refreshed,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
        var capability = new CapabilityCatalogEntry(
            service,
            "Sample",
            capabilityId,
            "Sample Capability",
            new Version(1, 0),
            new CapabilityResolutionResult(capabilityId, Enablement.Enabled, Availability.Unavailable(AvailabilityReason.MissingPrerequisite)));

        var projection = new BuildAtlasProjectionUseCase().Build([integration], [capability]);
        var node = Assert.Single(projection.Nodes, item => item.Kind == AtlasNodeKind.Capability);

        Assert.False(node.IsAvailable);
        Assert.Equal(AvailabilityReason.MissingPrerequisite, node.AvailabilityReason);
    }

    private static ServiceIntegrationListItem Integration(string id, string title) => new(
        new ServiceIdentity(id),
        title,
        true,
        null,
        true,
        true,
        true,
        IntegrationRefreshStatus.Refreshed,
        DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow);
}
