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
                    "capability:orientation:orientation.generic_geospatial",
                    AtlasNodeKind.Capability,
                    "Generic geospatial",
                    "Available",
                    new ServiceIdentity("orientation"),
                    new CapabilityIdentity("orientation.generic_geospatial"))
            ],
            [
                new AtlasConnection("composition:vocation", AtlasConnectionKind.Composition, "wgt.core", "service:vocation"),
                new AtlasConnection("composition:orientation", AtlasConnectionKind.Composition, "wgt.core", "service:orientation"),
                new AtlasConnection(
                    "ownership:orientation:geospatial",
                    AtlasConnectionKind.CapabilityOwnership,
                    "service:orientation",
                    "capability:orientation:orientation.generic_geospatial"),
                new AtlasConnection(
                    "dependency:vocation:orientation:geospatial",
                    AtlasConnectionKind.CapabilityDependency,
                    "service:vocation",
                    "capability:orientation:orientation.generic_geospatial",
                    "Vocation supplies work-location meaning while Orientation supplies generic geospatial capability.")
            ]);

        var layout = AtlasPresentationLayoutBuilder.Build(projection);
        var core = layout.Nodes.Single(node => node.NodeId == "wgt.core");
        var vocation = layout.Nodes.Single(node => node.NodeId == "service:vocation");
        var orientation = layout.Nodes.Single(node => node.NodeId == "service:orientation");
        var geospatial = layout.Nodes.Single(node => node.NodeId == "capability:orientation:orientation.generic_geospatial");

        Assert.Equal("2 products", core.ScopeSummaryText);
        Assert.Equal("0 capabilities", vocation.ScopeSummaryText);
        Assert.Equal("1 capability", orientation.ScopeSummaryText);
        Assert.Equal("System capability", geospatial.ScopeSummaryText);

        var outgoing = Assert.Single(vocation.Relationships);
        Assert.Equal("Uses", outgoing.Direction);
        Assert.Equal("Generic geospatial", outgoing.RelatedNodeTitle);
        Assert.Equal("capability:orientation:orientation.generic_geospatial", outgoing.RelatedNodeId);
        Assert.Equal("1 cross-service link", vocation.RelationshipSummaryText);

        var incoming = Assert.Single(geospatial.Relationships);
        Assert.Equal("Used by", incoming.Direction);
        Assert.Equal("service:vocation", incoming.RelatedNodeId);
        Assert.False(geospatial.HasNoRelationships);
        Assert.True(orientation.HasNoRelationships);
        Assert.Equal("No cross-service links", orientation.RelationshipSummaryText);
    }

    [Fact]
    public void Empty_core_reports_zero_products_without_inventing_relationships()
    {
        var layout = AtlasPresentationLayoutBuilder.Build(new AtlasProjection(
            [new AtlasNode("wgt.core", AtlasNodeKind.Core, "WGT", "System ready")],
            []));

        var core = Assert.Single(layout.Nodes);

        Assert.Equal("0 products", core.ScopeSummaryText);
        Assert.True(core.HasNoRelationships);
        Assert.Equal("No cross-service links", core.RelationshipSummaryText);
    }

    [Fact]
    public void Inspector_tether_is_not_driven_from_LayoutUpdated()
    {
        var root = FindRepositoryRoot();
        var sourcePath = Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.Polish.cs");
        var source = File.ReadAllText(sourcePath);

        Assert.DoesNotContain("InspectorCard.LayoutUpdated +=", source, StringComparison.Ordinal);
        Assert.Contains("InspectorCard.SizeChanged +=", source, StringComparison.Ordinal);
        Assert.Contains("inspectorTetherLayer = new Canvas", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "WiiiiGotThis.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Wiiii Got This repository root from the test output directory.");
    }
}
