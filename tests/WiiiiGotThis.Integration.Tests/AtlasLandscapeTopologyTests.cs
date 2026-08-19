using Avalonia;
using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;
using WiiiiGotThis.Presentation;

namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasLandscapeTopologyTests
{
    [Fact]
    public void First_class_services_form_districts_around_a_clear_core_nexus()
    {
        var layout = BuildRepresentativeLayout();

        var landscape = AtlasLandscapeBuilder.Build(layout.Nodes, layout.Connections);

        Assert.Equal(4, landscape.Regions.Count);
        Assert.DoesNotContain(landscape.Regions, region => region.NodeId == BuildAtlasProjectionUseCase.CoreNodeId);
        Assert.Equal(new Point(0, 0), landscape.Nexus);

        Assert.All(landscape.Regions, region =>
        {
            Assert.True(region.Contour.Count >= 12, $"{region.ServiceId} must have an authored district contour.");
            Assert.True(Distance(region.InnerGate, landscape.Nexus) >= 110, $"{region.ServiceId} must leave a real nexus plaza between district and Core.");
            Assert.True(Distance(region.InnerGate, landscape.Nexus) <= 180, $"{region.ServiceId} nexus gate must remain spatially connected to Core.");
            Assert.All(region.Contour, point =>
                Assert.True(Distance(point, landscape.Nexus) > 105, $"{region.ServiceId} contour must not cover the WGT nexus."));
        });

        Assert.Equal(4, landscape.Routes.Count(route => route.Kind == AtlasLandscapeRouteKind.CompositionCorridor));
        Assert.Equal(4, landscape.Gates.Count(gate => gate.Kind == AtlasLandscapeGateKind.NexusAccess));
    }

    [Fact]
    public void Cross_service_dependency_leaves_the_source_district_through_explicit_gates_and_routes_around_core()
    {
        var layout = BuildRepresentativeLayout();
        var landscape = AtlasLandscapeBuilder.Build(layout.Nodes, layout.Connections);

        var dependency = Assert.Single(landscape.Routes.Where(route => route.Kind == AtlasLandscapeRouteKind.CrossServiceDependency));
        var source = layout.Nodes.Single(node => node.NodeId == dependency.SourceNodeId);
        var target = layout.Nodes.Single(node => node.NodeId == dependency.TargetNodeId);

        Assert.True(dependency.Waypoints.Count >= 8);
        Assert.Equal(new Point(source.X, source.Y), dependency.Waypoints[0]);
        Assert.Equal(new Point(target.X, target.Y), dependency.Waypoints[^1]);
        Assert.Contains(dependency.Waypoints, point => point.Y >= 160);
        Assert.DoesNotContain(dependency.Waypoints.Skip(2).Take(dependency.Waypoints.Count - 4), point => Distance(point, landscape.Nexus) < 125);

        var routeGates = landscape.Gates.Where(gate => gate.RouteId == dependency.RouteId).ToArray();
        Assert.Equal(2, routeGates.Length);
        Assert.Contains(routeGates, gate => gate.Kind == AtlasLandscapeGateKind.DependencyEgress);
        Assert.Contains(routeGates, gate => gate.Kind == AtlasLandscapeGateKind.DependencyIngress);
    }

    [Fact]
    public void Capabilities_remain_local_district_landmarks_and_ownership_becomes_local_paths()
    {
        var layout = BuildRepresentativeLayout();
        var landscape = AtlasLandscapeBuilder.Build(layout.Nodes, layout.Connections);

        var vocation = landscape.Regions.Single(region => region.ServiceId == "vocation");
        Assert.Equal(2, vocation.CapabilityNodeIds.Count);

        var localPaths = landscape.Routes
            .Where(route => route.Kind == AtlasLandscapeRouteKind.DistrictPath)
            .Where(route => vocation.CapabilityNodeIds.Contains(route.TargetNodeId, StringComparer.Ordinal))
            .ToArray();

        Assert.Equal(vocation.CapabilityNodeIds.Count, localPaths.Length);
        Assert.All(localPaths, route => Assert.Equal(vocation.NodeId, route.SourceNodeId));
        Assert.All(localPaths, route => Assert.Equal(3, route.Waypoints.Count));
    }

    private static AtlasPresentationLayout BuildRepresentativeLayout()
    {
        var core = new AtlasNode("wgt.core", AtlasNodeKind.Core, "WGT", "System ready");
        var vocation = Service("vocation", "Vocation");
        var illumination = Service("illumination", "Illumination");
        var orientation = Service("orientation", "Orientation");
        var conveyance = Service("conveyance", "Conveyance");
        var opportunity = Capability("vocation", "vocation.opportunity_overview", "Opportunity Overview");
        var mapProjection = Capability("vocation", "vocation.map_projection", "Map Projection");

        var projection = new AtlasProjection(
            [core, vocation, illumination, orientation, conveyance, opportunity, mapProjection],
            [
                Composition(core, vocation),
                Composition(core, illumination),
                Composition(core, orientation),
                Composition(core, conveyance),
                Ownership(vocation, opportunity),
                Ownership(vocation, mapProjection),
                new AtlasConnection(
                    "dependency:vocation:vocation.map_projection:orientation",
                    AtlasConnectionKind.CapabilityDependency,
                    mapProjection.NodeId,
                    orientation.NodeId,
                    "Vocation uses Orientation for generic spatial rendering.")
            ]);

        return AtlasPresentationLayoutBuilder.Build(projection);
    }

    private static AtlasNode Service(string id, string title) => new(
        $"service:{id}",
        AtlasNodeKind.Service,
        title,
        "Composed",
        new ServiceIdentity(id));

    private static AtlasNode Capability(string serviceId, string capabilityId, string title) => new(
        $"capability:{serviceId}:{capabilityId}",
        AtlasNodeKind.Capability,
        title,
        "Available",
        new ServiceIdentity(serviceId),
        new CapabilityIdentity(capabilityId));

    private static AtlasConnection Composition(AtlasNode core, AtlasNode service) => new(
        $"composition:{service.ServiceIdentity!.Value.Value}",
        AtlasConnectionKind.Composition,
        core.NodeId,
        service.NodeId);

    private static AtlasConnection Ownership(AtlasNode service, AtlasNode capability) => new(
        $"ownership:{service.ServiceIdentity!.Value.Value}:{capability.CapabilityIdentity!.Value.Value}",
        AtlasConnectionKind.CapabilityOwnership,
        service.NodeId,
        capability.NodeId);

    private static double Distance(Point first, Point second)
    {
        var delta = first - second;
        return Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
    }
}
