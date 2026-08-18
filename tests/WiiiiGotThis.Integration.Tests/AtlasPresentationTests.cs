using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;
using WiiiiGotThis.Presentation;

namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasPresentationTests
{
    [Theory]
    [InlineData("vocation", "Vocation")]
    [InlineData("illumination", "Illumination")]
    [InlineData("orientation", "Orientation")]
    public void Integrated_first_class_service_can_enter_full_product_even_before_enablement(
        string serviceId,
        string title)
    {
        var model = new AtlasNode(
            $"service:{serviceId}",
            AtlasNodeKind.Service,
            title,
            "Disabled on this device",
            new ServiceIdentity(serviceId),
            IsEnabled: false,
            IsAvailable: false,
            IsIntegrated: true);

        var node = new AtlasNodePresentationViewModel(model, 0, 0);

        Assert.True(node.CanOpenProductSurface);
        Assert.Equal($"Enable & open {title}", node.OpenProductSurfaceLabel);
    }

    [Fact]
    public void Known_only_service_does_not_claim_a_product_surface()
    {
        var model = new AtlasNode(
            "service:future",
            AtlasNodeKind.Service,
            "Future",
            "Not composed on this client yet",
            new ServiceIdentity("future"),
            IsEnabled: false,
            IsAvailable: false,
            IsIntegrated: false);

        var node = new AtlasNodePresentationViewModel(model, 0, 0);

        Assert.False(node.CanOpenProductSurface);
    }

    [Fact]
    public void Focus_from_vocation_reveals_its_capability_dependency_path_without_unrelated_services()
    {
        var graph = FocusGraph();

        var focused = AtlasPresentationFocus.Build(graph.Connections, "service:vocation");

        Assert.Contains("wgt.core", focused);
        Assert.Contains("service:vocation", focused);
        Assert.Contains("capability:vocation:vocation.map_projection", focused);
        Assert.Contains("service:orientation", focused);
        Assert.DoesNotContain("service:illumination", focused);
        Assert.DoesNotContain("service:conveyance", focused);
    }

    [Fact]
    public void Focus_from_orientation_reveals_incoming_vocation_dependency_and_owner_path()
    {
        var graph = FocusGraph();

        var focused = AtlasPresentationFocus.Build(graph.Connections, "service:orientation");

        Assert.Contains("wgt.core", focused);
        Assert.Contains("service:orientation", focused);
        Assert.Contains("capability:vocation:vocation.map_projection", focused);
        Assert.Contains("service:vocation", focused);
        Assert.DoesNotContain("service:illumination", focused);
        Assert.DoesNotContain("service:conveyance", focused);
    }

    [Fact]
    public void Focus_from_core_stops_at_first_class_services_instead_of_expanding_every_dependency()
    {
        var graph = FocusGraph();

        var focused = AtlasPresentationFocus.Build(graph.Connections, "wgt.core");

        Assert.Contains("service:vocation", focused);
        Assert.Contains("service:orientation", focused);
        Assert.Contains("service:illumination", focused);
        Assert.Contains("service:conveyance", focused);
        Assert.DoesNotContain("capability:vocation:vocation.map_projection", focused);
    }

    private static AtlasPresentationLayout FocusGraph()
    {
        var core = Node("wgt.core", AtlasNodeKind.Core, "WGT");
        var vocation = Node("service:vocation", AtlasNodeKind.Service, "Vocation", "vocation");
        var orientation = Node("service:orientation", AtlasNodeKind.Service, "Orientation", "orientation");
        var illumination = Node("service:illumination", AtlasNodeKind.Service, "Illumination", "illumination");
        var conveyance = Node("service:conveyance", AtlasNodeKind.Service, "Conveyance", "conveyance");
        var map = new AtlasNodePresentationViewModel(
            new AtlasNode(
                "capability:vocation:vocation.map_projection",
                AtlasNodeKind.Capability,
                "Map Projection",
                "Available",
                new ServiceIdentity("vocation"),
                new CapabilityIdentity("vocation.map_projection")),
            0,
            0);

        AtlasConnectionPresentationViewModel Edge(
            string id,
            AtlasConnectionKind kind,
            AtlasNodePresentationViewModel source,
            AtlasNodePresentationViewModel target) =>
            new(new AtlasConnection(id, kind, source.NodeId, target.NodeId), source, target);

        return new AtlasPresentationLayout(
            [core, vocation, orientation, illumination, conveyance, map],
            [
                Edge("composition:vocation", AtlasConnectionKind.Composition, core, vocation),
                Edge("composition:orientation", AtlasConnectionKind.Composition, core, orientation),
                Edge("composition:illumination", AtlasConnectionKind.Composition, core, illumination),
                Edge("composition:conveyance", AtlasConnectionKind.Composition, core, conveyance),
                Edge("capability:vocation:map", AtlasConnectionKind.CapabilityOwnership, vocation, map),
                Edge("dependency:vocation:map:orientation", AtlasConnectionKind.CapabilityDependency, map, orientation)
            ]);
    }

    private static AtlasNodePresentationViewModel Node(
        string nodeId,
        AtlasNodeKind kind,
        string title,
        string? serviceId = null) =>
        new(
            new AtlasNode(
                nodeId,
                kind,
                title,
                "Available",
                serviceId is null ? null : new ServiceIdentity(serviceId)),
            0,
            0);
}
