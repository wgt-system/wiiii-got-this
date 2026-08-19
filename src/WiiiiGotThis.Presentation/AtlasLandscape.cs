using Avalonia;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

/// <summary>
/// Renderer-facing spatial topology. This is deliberately downstream of the Atlas
/// presentation projection: it gives accepted WGT semantics a stable geography without
/// making the renderer or the geography model business/domain authority.
/// </summary>
public sealed record AtlasLandscape(
    Point Nexus,
    IReadOnlyList<AtlasLandscapeRegion> Regions,
    IReadOnlyList<AtlasLandscapeGate> Gates,
    IReadOnlyList<AtlasLandscapeRoute> Routes,
    IReadOnlyDictionary<string, AtlasLandscapeLandmark> Landmarks);

public sealed record AtlasLandscapeRegion(
    string NodeId,
    string ServiceId,
    Point Landmark,
    Point InnerGate,
    Point LabelAnchor,
    IReadOnlyList<Point> Contour,
    IReadOnlyList<string> CapabilityNodeIds);

public sealed record AtlasLandscapeGate(
    string GateId,
    string ServiceNodeId,
    Point Position,
    AtlasLandscapeGateKind Kind,
    string? RouteId = null);

public enum AtlasLandscapeGateKind
{
    NexusAccess,
    DependencyEgress,
    DependencyIngress
}

public sealed record AtlasLandscapeRoute(
    string RouteId,
    AtlasLandscapeRouteKind Kind,
    string SourceNodeId,
    string TargetNodeId,
    IReadOnlyList<Point> Waypoints);

public enum AtlasLandscapeRouteKind
{
    CompositionCorridor,
    DistrictPath,
    CrossServiceDependency
}

public sealed record AtlasLandscapeLandmark(
    string NodeId,
    Point Position,
    AtlasNodeKind Kind,
    string? ServiceId);

public static class AtlasLandscapeBuilder
{
    private static readonly Point Nexus = new(0, 0);

    public static AtlasLandscape Build(
        IReadOnlyCollection<AtlasNodePresentationViewModel> nodes,
        IReadOnlyCollection<AtlasConnectionPresentationViewModel> connections)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(connections);

        var nodeById = nodes.ToDictionary(node => node.NodeId, StringComparer.Ordinal);
        var regions = nodes
            .Where(node => node.IsService)
            .Select(node => RegionFor(node, nodes))
            .ToArray();
        var regionByNodeId = regions.ToDictionary(region => region.NodeId, StringComparer.Ordinal);
        var regionByServiceId = regions.ToDictionary(region => region.ServiceId, StringComparer.Ordinal);

        var landmarks = nodes.ToDictionary(
            node => node.NodeId,
            node => new AtlasLandscapeLandmark(
                node.NodeId,
                new Point(node.X, node.Y),
                node.Kind,
                node.ServiceIdentity?.Value),
            StringComparer.Ordinal);

        var gates = new List<AtlasLandscapeGate>();
        var routes = new List<AtlasLandscapeRoute>();

        foreach (var region in regions)
        {
            gates.Add(new(
                $"gate:nexus:{region.ServiceId}",
                region.NodeId,
                region.InnerGate,
                AtlasLandscapeGateKind.NexusAccess));
        }

        foreach (var connection in connections)
        {
            switch (connection.Kind)
            {
                case AtlasConnectionKind.Composition:
                    if (regionByNodeId.TryGetValue(connection.Target.NodeId, out var compositionRegion))
                    {
                        routes.Add(new(
                            connection.Model.ConnectionId,
                            AtlasLandscapeRouteKind.CompositionCorridor,
                            connection.Source.NodeId,
                            connection.Target.NodeId,
                            CompositionWaypoints(compositionRegion)));
                    }
                    break;

                case AtlasConnectionKind.CapabilityOwnership:
                    if (connection.Source.ServiceIdentity?.Value is { } ownerServiceId
                        && regionByServiceId.TryGetValue(ownerServiceId, out var ownerRegion))
                    {
                        routes.Add(new(
                            connection.Model.ConnectionId,
                            AtlasLandscapeRouteKind.DistrictPath,
                            connection.Source.NodeId,
                            connection.Target.NodeId,
                            DistrictWaypoints(ownerRegion, connection.Target)));
                    }
                    break;

                case AtlasConnectionKind.CapabilityDependency:
                    if (connection.Source.ServiceIdentity?.Value is not { } sourceServiceId
                        || connection.Target.ServiceIdentity?.Value is not { } targetServiceId
                        || !regionByServiceId.TryGetValue(sourceServiceId, out var sourceRegion)
                        || !regionByServiceId.TryGetValue(targetServiceId, out var targetRegion)
                        || !nodeById.TryGetValue(connection.Source.NodeId, out var sourceNode)
                        || !nodeById.TryGetValue(connection.Target.NodeId, out var targetNode))
                    {
                        break;
                    }

                    var dependency = DependencyWaypoints(sourceRegion, targetRegion, sourceNode, targetNode);
                    routes.Add(new(
                        connection.Model.ConnectionId,
                        AtlasLandscapeRouteKind.CrossServiceDependency,
                        connection.Source.NodeId,
                        connection.Target.NodeId,
                        dependency.Waypoints));
                    gates.Add(new(
                        $"gate:dependency:egress:{connection.Model.ConnectionId}",
                        sourceRegion.NodeId,
                        dependency.SourceGate,
                        AtlasLandscapeGateKind.DependencyEgress,
                        connection.Model.ConnectionId));
                    gates.Add(new(
                        $"gate:dependency:ingress:{connection.Model.ConnectionId}",
                        targetRegion.NodeId,
                        dependency.TargetGate,
                        AtlasLandscapeGateKind.DependencyIngress,
                        connection.Model.ConnectionId));
                    break;
            }
        }

        return new(
            Nexus,
            regions,
            gates,
            routes,
            landmarks);
    }

    private static AtlasLandscapeRegion RegionFor(
        AtlasNodePresentationViewModel service,
        IReadOnlyCollection<AtlasNodePresentationViewModel> nodes)
    {
        var serviceId = service.ServiceIdentity?.Value ?? service.NodeId;
        var capabilities = nodes
            .Where(node => node.IsCapability && string.Equals(node.ServiceIdentity?.Value, serviceId, StringComparison.Ordinal))
            .Select(node => node.NodeId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        var authored = serviceId switch
        {
            "vocation" => new AuthoredRegion(
                new Point(-150, 0),
                new Point(-430, -198),
                [
                    new(-705, -92), new(-660, -190), new(-555, -242), new(-405, -250),
                    new(-265, -214), new(-172, -142), new(-138, -62), new(-154, 0),
                    new(-138, 66), new(-182, 144), new(-286, 212), new(-438, 246),
                    new(-592, 214), new(-688, 126)
                ]),
            "illumination" => new AuthoredRegion(
                new Point(0, -116),
                new Point(-202, -410),
                [
                    new(-248, -286), new(-220, -405), new(-132, -492), new(-8, -532),
                    new(124, -502), new(218, -414), new(246, -300), new(214, -204),
                    new(132, -142), new(56, -112), new(0, -122), new(-64, -112),
                    new(-148, -150), new(-216, -208)
                ]),
            "orientation" => new AuthoredRegion(
                new Point(150, 0),
                new Point(420, -202),
                [
                    new(138, -62), new(172, -142), new(268, -214), new(410, -248),
                    new(562, -224), new(672, -160), new(714, -58), new(702, 70),
                    new(636, 164), new(522, 224), new(374, 242), new(244, 202),
                    new(170, 132), new(140, 60), new(154, 0)
                ]),
            "conveyance" => new AuthoredRegion(
                new Point(0, 116),
                new Point(-214, 414),
                [
                    new(-62, 112), new(0, 122), new(62, 112), new(148, 150),
                    new(220, 214), new(250, 314), new(224, 420), new(134, 502),
                    new(12, 534), new(-126, 504), new(-220, 416), new(-252, 306),
                    new(-214, 206), new(-134, 142)
                ]),
            _ => FallbackRegion(service)
        };

        return new(
            service.NodeId,
            serviceId,
            new Point(service.X, service.Y),
            authored.InnerGate,
            authored.LabelAnchor,
            authored.Contour,
            capabilities);
    }

    private static AuthoredRegion FallbackRegion(AtlasNodePresentationViewModel service)
    {
        var center = new Point(service.X, service.Y);
        const double rx = 180;
        const double ry = 130;
        var direction = Normalize(center);
        var innerGate = center - direction * 122;
        return new(
            innerGate,
            center + new Vector(-rx * 0.60, -ry * 0.76),
            [
                center + new Vector(-rx, -ry * 0.10),
                center + new Vector(-rx * 0.68, -ry * 0.82),
                center + new Vector(0, -ry),
                center + new Vector(rx * 0.72, -ry * 0.72),
                center + new Vector(rx, 0),
                center + new Vector(rx * 0.68, ry * 0.82),
                center + new Vector(0, ry),
                center + new Vector(-rx * 0.72, ry * 0.72)
            ]);
    }

    private static IReadOnlyList<Point> CompositionWaypoints(AtlasLandscapeRegion region)
    {
        var direction = Normalize(region.InnerGate);
        var nexusGate = new Point(direction.X * 78, direction.Y * 78);
        var exchange = new Point(direction.X * 108, direction.Y * 108);
        return [Nexus, nexusGate, exchange, region.InnerGate, region.Landmark];
    }

    private static IReadOnlyList<Point> DistrictWaypoints(
        AtlasLandscapeRegion region,
        AtlasNodePresentationViewModel capability)
    {
        var capabilityPoint = new Point(capability.X, capability.Y);
        var outward = Normalize(region.Landmark);
        var lateral = new Vector(-outward.Y, outward.X);
        var signed = StableSide(capability.NodeId);
        var junction = region.Landmark + outward * 52 + lateral * (signed * 18);
        return [region.Landmark, junction, capabilityPoint];
    }

    private static DependencyPath DependencyWaypoints(
        AtlasLandscapeRegion sourceRegion,
        AtlasLandscapeRegion targetRegion,
        AtlasNodePresentationViewModel sourceNode,
        AtlasNodePresentationViewModel targetNode)
    {
        if (string.Equals(sourceRegion.ServiceId, "vocation", StringComparison.Ordinal)
            && string.Equals(targetRegion.ServiceId, "orientation", StringComparison.Ordinal))
        {
            var sourceGate = new Point(-168, 126);
            var targetGate = new Point(168, 126);
            return new(
                sourceGate,
                targetGate,
                [
                    new Point(sourceNode.X, sourceNode.Y),
                    new Point(-470, 120),
                    sourceGate,
                    new Point(-82, 166),
                    new Point(0, 176),
                    new Point(82, 166),
                    targetGate,
                    new Point(258, 118),
                    new Point(targetNode.X, targetNode.Y)
                ]);
        }

        var sourceDirection = Normalize(sourceRegion.Landmark);
        var targetDirection = Normalize(targetRegion.Landmark);
        var sourceGateGeneric = sourceRegion.InnerGate + new Vector(-sourceDirection.Y, sourceDirection.X) * 34;
        var targetGateGeneric = targetRegion.InnerGate - new Vector(-targetDirection.Y, targetDirection.X) * 34;
        var sourceExchange = new Point(sourceDirection.X * 154, sourceDirection.Y * 154);
        var targetExchange = new Point(targetDirection.X * 154, targetDirection.Y * 154);

        return new(
            sourceGateGeneric,
            targetGateGeneric,
            [
                new Point(sourceNode.X, sourceNode.Y),
                sourceGateGeneric,
                sourceExchange,
                targetExchange,
                targetGateGeneric,
                new Point(targetNode.X, targetNode.Y)
            ]);
    }

    private static Vector Normalize(Point point)
    {
        var length = Math.Sqrt(point.X * point.X + point.Y * point.Y);
        return length < 0.001
            ? new Vector(0, -1)
            : new Vector(point.X / length, point.Y / length);
    }

    private static double StableSide(string value)
    {
        var checksum = 0;
        foreach (var character in value)
            checksum = (checksum + character) % 2;
        return checksum == 0 ? 1d : -1d;
    }

    private sealed record AuthoredRegion(
        Point InnerGate,
        Point LabelAnchor,
        IReadOnlyList<Point> Contour);

    private sealed record DependencyPath(
        Point SourceGate,
        Point TargetGate,
        IReadOnlyList<Point> Waypoints);
}
