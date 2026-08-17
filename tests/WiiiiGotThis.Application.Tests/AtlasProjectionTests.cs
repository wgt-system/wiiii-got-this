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
        var integration = new ServiceIntegrationListItem(vocation, "Vocation", true, null, true, true, true, IntegrationRefreshStatus.Refreshed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var capability = new CapabilityCatalogEntry(vocation, "Vocation", opportunityOverview, "Opportunity Overview", new Version(1, 0), new CapabilityResolutionResult(opportunityOverview, Enablement.Enabled, Availability.Available));

        var projection = new BuildAtlasProjectionUseCase([]).Build([integration], [capability]);

        Assert.Equal(3, projection.Nodes.Count);
        Assert.Contains(projection.Nodes, node => node.NodeId == "wgt.core");
        Assert.Contains(projection.Nodes, node => node.NodeId == "service:vocation" && node.IsIntegrated && node.IsAvailable);
        Assert.Contains(projection.Nodes, node => node.NodeId == "capability:vocation:vocation.opportunity_overview" && node.IsAvailable);
        Assert.Contains(projection.Connections, edge => edge.Kind == AtlasConnectionKind.Composition && edge.SourceNodeId == "wgt.core" && edge.TargetNodeId == "service:vocation");
        Assert.Contains(projection.Connections, edge => edge.Kind == AtlasConnectionKind.CapabilityOwnership && edge.SourceNodeId == "service:vocation" && edge.TargetNodeId == "capability:vocation:vocation.opportunity_overview");
    }

    [Fact]
    public void Build_includes_first_class_product_services_without_inventing_capabilities()
    {
        var projection = new BuildAtlasProjectionUseCase().Build([], []);

        Assert.Equal(["wgt.core", "service:illumination", "service:orientation", "service:vocation"], projection.Nodes.Select(node => node.NodeId));
        Assert.All(projection.Nodes.Where(node => node.Kind == AtlasNodeKind.Service), node =>
        {
            Assert.False(node.IsIntegrated);
            Assert.False(node.IsAvailable);
            Assert.Equal("Not composed on this client yet", node.Subtitle);
        });
        Assert.DoesNotContain(projection.Nodes, node => node.Kind == AtlasNodeKind.Capability);
    }

    [Fact]
    public void Build_merges_real_integration_state_into_known_product_service()
    {
        var projection = new BuildAtlasProjectionUseCase().Build([Integration("vocation", "External label")], []);
        var vocation = Assert.Single(projection.Nodes, node => node.NodeId == "service:vocation");

        Assert.Equal("Vocation", vocation.Title);
        Assert.True(vocation.IsIntegrated);
        Assert.True(vocation.IsAvailable);
        Assert.NotNull(vocation.Description);
    }

    [Fact]
    public void Build_orders_services_stably_by_product_title()
    {
        var projection = new BuildAtlasProjectionUseCase().Build([
            Integration("vocation", "Vocation"),
            Integration("orientation", "Orientation"),
            Integration("illumination", "Illumination")], []);

        Assert.Equal(["wgt.core", "service:illumination", "service:orientation", "service:vocation"], projection.Nodes.Select(node => node.NodeId));
    }

    [Fact]
    public void Build_hides_developer_reference_integration_from_normal_Atlas()
    {
        var projection = new BuildAtlasProjectionUseCase([]).Build([Integration("reference-service", "Reference Integration")], []);

        Assert.Single(projection.Nodes);
        Assert.Equal(AtlasNodeKind.Core, projection.Nodes[0].Kind);
        Assert.Empty(projection.Connections);
    }

    [Fact]
    public void Build_can_include_reference_integration_for_developer_diagnostics()
    {
        var projection = new BuildAtlasProjectionUseCase([]).Build([Integration("reference-service", "Reference Integration")], [], includeDeveloperIntegrations: true);

        Assert.Contains(projection.Nodes, node => node.NodeId == "service:reference-service");
    }

    [Fact]
    public void Build_preserves_unavailable_capability_reason_without_inventing_product_semantics()
    {
        var service = new ServiceIdentity("sample");
        var capabilityId = new CapabilityIdentity("sample.capability");
        var integration = Integration("sample", "Sample");
        var capability = new CapabilityCatalogEntry(service, "Sample", capabilityId, "Sample Capability", new Version(1, 0), new CapabilityResolutionResult(capabilityId, Enablement.Enabled, Availability.Unavailable(AvailabilityReason.MissingPrerequisite)));

        var projection = new BuildAtlasProjectionUseCase([]).Build([integration], [capability]);
        var node = Assert.Single(projection.Nodes, item => item.Kind == AtlasNodeKind.Capability);

        Assert.False(node.IsAvailable);
        Assert.Equal(AvailabilityReason.MissingPrerequisite, node.AvailabilityReason);
    }

    private static ServiceIntegrationListItem Integration(string id, string title) => new(
        new ServiceIdentity(id), title, true, null, true, true, true,
        IntegrationRefreshStatus.Refreshed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
}
