using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;
using WiiiiGotThis.Presentation;

namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasSpatialCompositionTests
{
    [Fact]
    public void Primary_services_use_a_widescreen_elliptical_cross()
    {
        var projection = new AtlasProjection(
            [
                new AtlasNode("wgt.core", AtlasNodeKind.Core, "WGT", "System ready"),
                Service("illumination", "Illumination"),
                Service("orientation", "Orientation"),
                Service("conveyance", "Conveyance"),
                Service("vocation", "Vocation")
            ],
            []);

        var layout = AtlasPresentationLayoutBuilder.Build(projection);

        AssertPosition(layout, "service:illumination", 0, -285);
        AssertPosition(layout, "service:orientation", 365, 0);
        AssertPosition(layout, "service:conveyance", 0, 285);
        AssertPosition(layout, "service:vocation", -365, 0);
    }

    [Fact]
    public void Capability_ports_stay_clustered_close_to_their_provider()
    {
        var vocation = Service("vocation", "Vocation");
        var capability = new AtlasNode(
            "capability:vocation:vocation.map_projection",
            AtlasNodeKind.Capability,
            "Map Projection",
            "Available",
            new ServiceIdentity("vocation"),
            new CapabilityIdentity("vocation.map_projection"));
        var projection = new AtlasProjection(
            [
                new AtlasNode("wgt.core", AtlasNodeKind.Core, "WGT", "System ready"),
                vocation,
                capability
            ],
            [
                new AtlasConnection(
                    "ownership:vocation:map",
                    AtlasConnectionKind.CapabilityOwnership,
                    vocation.NodeId,
                    capability.NodeId)
            ]);

        var layout = AtlasPresentationLayoutBuilder.Build(projection);
        var service = layout.Nodes.Single(node => node.NodeId == vocation.NodeId);
        var port = layout.Nodes.Single(node => node.NodeId == capability.NodeId);
        var distance = Math.Sqrt(
            Math.Pow(port.X - service.X, 2) +
            Math.Pow(port.Y - service.Y, 2));

        Assert.InRange(distance, 131.999, 132.001);
    }

    private static AtlasNode Service(string id, string title) => new(
        $"service:{id}",
        AtlasNodeKind.Service,
        title,
        "Composed",
        new ServiceIdentity(id));

    private static void AssertPosition(AtlasPresentationLayout layout, string nodeId, double x, double y)
    {
        var node = layout.Nodes.Single(item => item.NodeId == nodeId);
        Assert.Equal(x, node.X, precision: 6);
        Assert.Equal(y, node.Y, precision: 6);
    }
}
