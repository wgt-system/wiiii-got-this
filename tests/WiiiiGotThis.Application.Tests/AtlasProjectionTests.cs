using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Application.Tests;

public sealed class AtlasProjectionTests
{
    [Fact]
    public void Build_projects_published_capabilities_for_nonproduct_integrations()
    {
        var sample = new ServiceIdentity("sample");
        var sampleCapability = new CapabilityIdentity("sample.capability");
        var integration = new ServiceIntegrationListItem(sample, "Sample", true, null, true, true, true, IntegrationRefreshStatus.Refreshed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var capability = new CapabilityCatalogEntry(sample, "Sample", sampleCapability, "Sample Capability", new Version(1, 0), new CapabilityResolutionResult(sampleCapability, Enablement.Enabled, Availability.Available));

        var projection = new BuildAtlasProjectionUseCase([], []).Build([integration], [capability]);

        Assert.Equal(3, projection.Nodes.Count);
        Assert.Contains(projection.Nodes, node => node.NodeId == "wgt.core");
        Assert.Contains(projection.Nodes, node => node.NodeId == "service:sample" && node.IsIntegrated && node.IsAvailable);
        Assert.Contains(projection.Nodes, node => node.NodeId == "capability:sample:sample.capability" && node.IsAvailable);
        Assert.Contains(projection.Connections, edge => edge.Kind == AtlasConnectionKind.Composition && edge.SourceNodeId == "wgt.core" && edge.TargetNodeId == "service:sample");
        Assert.Contains(projection.Connections, edge => edge.Kind == AtlasConnectionKind.CapabilityOwnership && edge.SourceNodeId == "service:sample" && edge.TargetNodeId == "capability:sample:sample.capability");
    }

    [Fact]
    public void Build_includes_known_products_shared_infrastructure_and_declared_system_capabilities()
    {
        var projection = new BuildAtlasProjectionUseCase().Build([], []);

        Assert.Equal([
            "wgt.core",
            "service:conveyance",
            "service:illumination",
            "service:orientation",
            "service:vocation",
            "capability:orientation:orientation.generic_geospatial",
            "capability:conveyance:conveyance.durable_delivery"], projection.Nodes.Select(node => node.NodeId));

        Assert.All(projection.Nodes.Where(node => node.Kind == AtlasNodeKind.Service), node =>
        {
            Assert.False(node.IsIntegrated);
            Assert.False(node.IsAvailable);
            Assert.Equal("Not composed on this client yet", node.Subtitle);
        });

        var geospatial = Assert.Single(projection.Nodes, node => node.CapabilityIdentity?.Value == BuildAtlasProjectionUseCase.OrientationGeospatialCapabilityId);
        Assert.Equal("orientation", geospatial.ServiceIdentity?.Value);
        Assert.Equal("Generic geospatial", geospatial.Title);
        Assert.False(geospatial.IsAvailable);

        var delivery = Assert.Single(projection.Nodes, node => node.CapabilityIdentity?.Value == BuildAtlasProjectionUseCase.ConveyanceDurableDeliveryCapabilityId);
        Assert.Equal("conveyance", delivery.ServiceIdentity?.Value);
        Assert.Equal("Cross-device delivery", delivery.Title);
        Assert.False(delivery.IsAvailable);

        Assert.Equal(AtlasProductRole.FirstClassProductProvider, projection.Nodes.Single(node => node.NodeId == "service:vocation").ProductRole);
        Assert.Equal(AtlasProductRole.FirstClassProductProvider, projection.Nodes.Single(node => node.NodeId == "service:illumination").ProductRole);
        Assert.Equal(AtlasProductRole.DualRoleProvider, projection.Nodes.Single(node => node.NodeId == "service:orientation").ProductRole);
        Assert.Equal(AtlasProductRole.SharedCapabilityProvider, projection.Nodes.Single(node => node.NodeId == "service:conveyance").ProductRole);
    }

    [Fact]
    public void Build_keeps_Conveyance_truthful_without_projecting_it_as_a_peer_product()
    {
        var projection = new BuildAtlasProjectionUseCase().Build([], []);
        var conveyance = Assert.Single(projection.Nodes, node => node.NodeId == "service:conveyance");
        var delivery = Assert.Single(projection.Nodes, node => node.CapabilityIdentity?.Value == BuildAtlasProjectionUseCase.ConveyanceDurableDeliveryCapabilityId);

        Assert.Equal("Conveyance", conveyance.Title);
        Assert.Equal(AtlasProductRole.SharedCapabilityProvider, conveyance.ProductRole);
        Assert.False(conveyance.IsIntegrated);
        Assert.False(conveyance.IsAvailable);
        Assert.Equal(conveyance.ServiceIdentity, delivery.ServiceIdentity);
        Assert.DoesNotContain(projection.Connections, edge =>
            edge.Kind == AtlasConnectionKind.Composition
            && edge.TargetNodeId == conveyance.NodeId);
    }

    [Fact]
    public void Build_composes_the_three_current_first_class_products_with_WGT_core()
    {
        var projection = new BuildAtlasProjectionUseCase().Build([], []);
        var productTargets = projection.Connections
            .Where(edge => edge.Kind == AtlasConnectionKind.Composition)
            .Select(edge => edge.TargetNodeId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal([
            "service:illumination",
            "service:orientation",
            "service:vocation"], productTargets);
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
        Assert.Equal(AtlasProductRole.FirstClassProductProvider, vocation.ProductRole);
    }

    [Fact]
    public void Build_does_not_mirror_Vocation_integration_contracts_into_global_Atlas_capabilities()
    {
        var vocation = new ServiceIdentity("vocation");
        var opportunity = new CapabilityIdentity("vocation.opportunity_overview");
        var mapProjection = new CapabilityIdentity("vocation.map_projection");
        var capabilities = new[]
        {
            new CapabilityCatalogEntry(vocation, "Vocation", opportunity, "Opportunity Overview", new Version(1, 0), new CapabilityResolutionResult(opportunity, Enablement.Enabled, Availability.Available)),
            new CapabilityCatalogEntry(vocation, "Vocation", mapProjection, "Map Projection", new Version(1, 0), new CapabilityResolutionResult(mapProjection, Enablement.Enabled, Availability.Available))
        };

        var projection = new BuildAtlasProjectionUseCase().Build([Integration("vocation", "Vocation")], capabilities);

        Assert.DoesNotContain(projection.Nodes, node => node.CapabilityIdentity?.Value == "vocation.opportunity_overview");
        Assert.DoesNotContain(projection.Nodes, node => node.CapabilityIdentity?.Value == "vocation.map_projection");
        Assert.Contains(projection.Nodes, node => node.CapabilityIdentity?.Value == BuildAtlasProjectionUseCase.OrientationGeospatialCapabilityId);
        Assert.Contains(projection.Nodes, node => node.CapabilityIdentity?.Value == BuildAtlasProjectionUseCase.ConveyanceDurableDeliveryCapabilityId);
    }

    [Fact]
    public void Build_models_Vocation_as_consumer_of_Orientation_and_Conveyance_owned_capabilities()
    {
        var projection = new BuildAtlasProjectionUseCase().Build([Integration("vocation", "Vocation")], []);
        var geospatial = Assert.Single(projection.Nodes, node => node.CapabilityIdentity?.Value == BuildAtlasProjectionUseCase.OrientationGeospatialCapabilityId);
        var delivery = Assert.Single(projection.Nodes, node => node.CapabilityIdentity?.Value == BuildAtlasProjectionUseCase.ConveyanceDurableDeliveryCapabilityId);
        var ownership = projection.Connections.Where(edge => edge.Kind == AtlasConnectionKind.CapabilityOwnership).ToArray();
        var dependencies = projection.Connections.Where(edge => edge.Kind == AtlasConnectionKind.CapabilityDependency).ToArray();

        Assert.Contains(ownership, edge => edge.SourceNodeId == "service:orientation" && edge.TargetNodeId == geospatial.NodeId);
        Assert.Contains(ownership, edge => edge.SourceNodeId == "service:conveyance" && edge.TargetNodeId == delivery.NodeId);
        Assert.Contains(dependencies, edge => edge.SourceNodeId == "service:vocation" && edge.TargetNodeId == geospatial.NodeId && edge.Description!.Contains("Orientation", StringComparison.Ordinal));
        Assert.Contains(dependencies, edge => edge.SourceNodeId == "service:vocation" && edge.TargetNodeId == delivery.NodeId && edge.Description!.Contains("Conveyance", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_orders_services_stably_by_product_title()
    {
        var projection = new BuildAtlasProjectionUseCase().Build([
            Integration("vocation", "Vocation"),
            Integration("orientation", "Orientation"),
            Integration("illumination", "Illumination")], []);

        Assert.Equal([
            "wgt.core",
            "service:conveyance",
            "service:illumination",
            "service:orientation",
            "service:vocation",
            "capability:orientation:orientation.generic_geospatial",
            "capability:conveyance:conveyance.durable_delivery"], projection.Nodes.Select(node => node.NodeId));
    }

    [Fact]
    public void Build_hides_developer_reference_integration_from_normal_Atlas()
    {
        var projection = new BuildAtlasProjectionUseCase([], []).Build([Integration("reference-service", "Reference Integration")], []);

        Assert.Single(projection.Nodes);
        Assert.Equal(AtlasNodeKind.Core, projection.Nodes[0].Kind);
        Assert.Empty(projection.Connections);
    }

    [Fact]
    public void Build_can_include_reference_integration_for_developer_diagnostics()
    {
        var projection = new BuildAtlasProjectionUseCase([], []).Build([Integration("reference-service", "Reference Integration")], [], includeDeveloperIntegrations: true);

        Assert.Contains(projection.Nodes, node => node.NodeId == "service:reference-service");
    }

    [Fact]
    public void Build_preserves_unavailable_capability_reason_for_nonproduct_integration()
    {
        var service = new ServiceIdentity("sample");
        var capabilityId = new CapabilityIdentity("sample.capability");
        var integration = Integration("sample", "Sample");
        var capability = new CapabilityCatalogEntry(service, "Sample", capabilityId, "Sample Capability", new Version(1, 0), new CapabilityResolutionResult(capabilityId, Enablement.Enabled, Availability.Unavailable(AvailabilityReason.MissingPrerequisite)));

        var projection = new BuildAtlasProjectionUseCase([], []).Build([integration], [capability]);
        var node = Assert.Single(projection.Nodes, item => item.Kind == AtlasNodeKind.Capability);

        Assert.False(node.IsAvailable);
        Assert.Equal(AvailabilityReason.MissingPrerequisite, node.AvailabilityReason);
    }

    private static ServiceIntegrationListItem Integration(string id, string title) => new(
        new ServiceIdentity(id), title, true, null, true, true, true,
        IntegrationRefreshStatus.Refreshed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
}
