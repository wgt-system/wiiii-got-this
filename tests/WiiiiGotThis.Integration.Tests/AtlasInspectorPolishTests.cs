using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;
using WiiiiGotThis.Presentation;

namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasInspectorPolishTests
{
    [Fact]
    public void Layout_exposes_scope_summaries_and_navigable_dependency_targets()
    {
        var projection = new AtlasProjection(
            [
                new AtlasNode("wgt.core", AtlasNodeKind.Core, "WGT", "System ready"),
                new AtlasNode("service:vocation", AtlasNodeKind.Service, "Vocation", "Composed", new ServiceIdentity("vocation")),
                new AtlasNode("service:orientation", AtlasNodeKind.Service, "Orientation", "Composed", new ServiceIdentity("orientation")),
                new AtlasNode(
                    "capability:vocation:vocation.map_projection",
                    AtlasNodeKind.Capability,
                    "Map Projection",
                    "Available",
                    new ServiceIdentity("vocation"),
                    new CapabilityIdentity("vocation.map_projection"))
            ],
            [
                new AtlasConnection("composition:vocation", AtlasConnectionKind.Composition, "wgt.core", "service:vocation"),
                new AtlasConnection("composition:orientation", AtlasConnectionKind.Composition, "wgt.core", "service:orientation"),
                new AtlasConnection(
                    "ownership:vocation:map",
                    AtlasConnectionKind.CapabilityOwnership,
                    "service:vocation",
                    "capability:vocation:vocation.map_projection"),
                new AtlasConnection(
                    "dependency:vocation:map:orientation",
                    AtlasConnectionKind.CapabilityDependency,
                    "capability:vocation:vocation.map_projection",
                    "service:orientation",
                    "Vocation supplies work-location meaning while Orientation supplies generic geospatial capability.")
            ]);

        var layout = AtlasPresentationLayoutBuilder.Build(projection);
        var core = layout.Nodes.Single(node => node.NodeId == "wgt.core");
        var vocation = layout.Nodes.Single(node => node.NodeId == "service:vocation");
        var orientation = layout.Nodes.Single(node => node.NodeId == "service:orientation");
        var map = layout.Nodes.Single(node => node.NodeId == "capability:vocation:vocation.map_projection");

        Assert.Equal("2 services", core.ScopeSummaryText);
        Assert.Equal("1 capability", vocation.ScopeSummaryText);
        Assert.Equal("0 capabilities", orientation.ScopeSummaryText);
        Assert.Equal("Published capability", map.ScopeSummaryText);

        var outgoing = Assert.Single(map.Relationships);
        Assert.Equal("Uses", outgoing.Direction);
        Assert.Equal("Orientation", outgoing.RelatedNodeTitle);
        Assert.Equal("service:orientation", outgoing.RelatedNodeId);
        Assert.Equal("1 cross-service link", map.RelationshipSummaryText);

        var incoming = Assert.Single(orientation.Relationships);
        Assert.Equal("Used by", incoming.Direction);
        Assert.Equal("capability:vocation:vocation.map_projection", incoming.RelatedNodeId);
        Assert.False(orientation.HasNoRelationships);
        Assert.True(vocation.HasNoRelationships);
        Assert.Equal("No cross-service links", vocation.RelationshipSummaryText);
    }
}
