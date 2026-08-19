using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Presentation;

public sealed record AtlasRelationshipPresentationViewModel(
    string Direction,
    string RelatedNodeTitle,
    string RelatedNodeId,
    string Description);

public sealed class AtlasNodePresentationViewModel(AtlasNode node, double x, double y)
{
    private readonly List<AtlasRelationshipPresentationViewModel> relationships = [];

    public AtlasNode Model { get; } = node;
    public string NodeId => Model.NodeId;
    public AtlasNodeKind Kind => Model.Kind;
    public string Title => Model.Title;
    public string Subtitle => Model.Subtitle;
    public string? Description => Model.Description;
    public bool HasDescription => !string.IsNullOrWhiteSpace(Model.Description);
    public ServiceIdentity? ServiceIdentity => Model.ServiceIdentity;
    public CapabilityIdentity? CapabilityIdentity => Model.CapabilityIdentity;
    public AtlasProductRole? ProductRole => Model.ProductRole;
    public bool IsEnabled => Model.IsEnabled;
    public bool IsAvailable => Model.IsAvailable;
    public bool IsIntegrated => Model.IsIntegrated;
    public bool IsKnownOnlyService => IsService && !IsIntegrated;
    public bool IsIntegratedService => IsService && IsIntegrated;
    public bool IsFirstClassProductProvider => IsService && ProductRole == AtlasProductRole.FirstClassProductProvider;
    public bool IsDualRoleProvider => IsService && ProductRole == AtlasProductRole.DualRoleProvider;
    public bool IsSharedCapabilityProvider => IsService && ProductRole == AtlasProductRole.SharedCapabilityProvider;
    public bool IsPrimaryProductProvider => IsService && !IsSharedCapabilityProvider;
    public bool CanOpenProductSurface => IsIntegratedService && IsSupportedProductSurfaceService(ServiceIdentity?.Value);
    public string OpenProductSurfaceLabel => IsEnabled ? $"Open {Title}" : $"Enable & open {Title}";
    public AvailabilityReason? AvailabilityReason => Model.AvailabilityReason;
    public double X { get; } = x;
    public double Y { get; } = y;
    public bool IsCore => Kind == AtlasNodeKind.Core;
    public bool IsService => Kind == AtlasNodeKind.Service;
    public bool IsCapability => Kind == AtlasNodeKind.Capability;
    public IReadOnlyList<AtlasRelationshipPresentationViewModel> Relationships => relationships;
    public bool HasRelationships => relationships.Count > 0;
    public bool HasNoRelationships => relationships.Count == 0;
    public int ChildNodeCount { get; internal set; }
    public string ScopeSummaryText => Kind switch
    {
        AtlasNodeKind.Core => ChildNodeCount == 1 ? "1 product" : $"{ChildNodeCount} products",
        AtlasNodeKind.Service => ChildNodeCount == 1 ? "1 capability" : $"{ChildNodeCount} capabilities",
        AtlasNodeKind.Capability => "System capability",
        _ => "Atlas node"
    };
    public string RelationshipSummaryText => relationships.Count switch
    {
        0 => "No cross-service links",
        1 => "1 cross-service link",
        _ => $"{relationships.Count} cross-service links"
    };
    public string KindLabel => Kind switch
    {
        AtlasNodeKind.Core => "WGT CORE",
        AtlasNodeKind.Service when IsSharedCapabilityProvider => "SHARED INFRASTRUCTURE",
        AtlasNodeKind.Service when IsDualRoleProvider => "PRODUCT + CAPABILITY",
        AtlasNodeKind.Service => "PRODUCT",
        AtlasNodeKind.Capability => "CAPABILITY",
        _ => "NODE"
    };
    public string AvailabilityText => IsAvailable ? Subtitle : AvailabilityReason switch
    {
        WiiiiGotThis.Domain.AvailabilityReason.Disabled => "Disabled on this device",
        WiiiiGotThis.Domain.AvailabilityReason.Unreachable => "Provider unavailable",
        WiiiiGotThis.Domain.AvailabilityReason.Incompatible => "Incompatible contract",
        WiiiiGotThis.Domain.AvailabilityReason.Unsupported => "Unsupported here",
        WiiiiGotThis.Domain.AvailabilityReason.MissingPrerequisite => "Missing prerequisite",
        _ => Subtitle
    };
    public string CompactStateText => Kind switch
    {
        AtlasNodeKind.Core => "SYSTEM",
        AtlasNodeKind.Capability when IsAvailable => "READY",
        AtlasNodeKind.Capability => "OFFLINE",
        AtlasNodeKind.Service when !IsIntegrated => "KNOWN",
        AtlasNodeKind.Service when !IsEnabled => "OFF",
        AtlasNodeKind.Service when IsAvailable => "COMPOSED",
        AtlasNodeKind.Service => "DEGRADED",
        _ => string.Empty
    };

    internal void AddRelationship(AtlasRelationshipPresentationViewModel relationship) => relationships.Add(relationship);

    private static bool IsSupportedProductSurfaceService(string? serviceId) =>
        string.Equals(serviceId, "vocation", StringComparison.Ordinal)
        || string.Equals(serviceId, "illumination", StringComparison.Ordinal)
        || string.Equals(serviceId, "orientation", StringComparison.Ordinal);
}

public sealed class AtlasConnectionPresentationViewModel(
    AtlasConnection model,
    AtlasNodePresentationViewModel source,
    AtlasNodePresentationViewModel target)
{
    public AtlasConnection Model { get; } = model;
    public AtlasConnectionKind Kind => Model.Kind;
    public AtlasNodePresentationViewModel Source { get; } = source;
    public AtlasNodePresentationViewModel Target { get; } = target;
}

public sealed record AtlasPresentationLayout(
    IReadOnlyList<AtlasNodePresentationViewModel> Nodes,
    IReadOnlyList<AtlasConnectionPresentationViewModel> Connections);

public static class AtlasPresentationFocus
{
    public static IReadOnlySet<string> Build(
        IReadOnlyCollection<AtlasConnectionPresentationViewModel> connections,
        string? selectedNodeId)
    {
        ArgumentNullException.ThrowIfNull(connections);

        var focused = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(selectedNodeId))
            return focused;

        focused.Add(selectedNodeId);
        foreach (var connection in connections)
        {
            if (string.Equals(connection.Source.NodeId, selectedNodeId, StringComparison.Ordinal)
                || string.Equals(connection.Target.NodeId, selectedNodeId, StringComparison.Ordinal))
            {
                focused.Add(connection.Source.NodeId);
                focused.Add(connection.Target.NodeId);
            }
        }

        if (string.Equals(selectedNodeId, BuildAtlasProjectionUseCase.CoreNodeId, StringComparison.Ordinal))
            return focused;

        foreach (var connection in connections.Where(item =>
                     item.Kind is AtlasConnectionKind.CapabilityConsumption or AtlasConnectionKind.CapabilityDependency))
        {
            if (focused.Contains(connection.Source.NodeId) || focused.Contains(connection.Target.NodeId))
            {
                focused.Add(connection.Source.NodeId);
                focused.Add(connection.Target.NodeId);
            }
        }

        foreach (var connection in connections.Where(item => item.Kind == AtlasConnectionKind.CapabilityOwnership))
        {
            if (focused.Contains(connection.Target.NodeId))
                focused.Add(connection.Source.NodeId);
        }

        foreach (var connection in connections.Where(item => item.Kind == AtlasConnectionKind.Composition))
        {
            if (focused.Contains(connection.Target.NodeId))
                focused.Add(connection.Source.NodeId);
        }

        return focused;
    }
}

public static class AtlasPresentationLayoutBuilder
{
    private static readonly Dictionary<string, int> PrimaryServiceOrder = new(StringComparer.Ordinal)
    {
        ["illumination"] = 0,
        ["orientation"] = 1,
        ["conveyance"] = 2,
        ["vocation"] = 3
    };

    public static AtlasPresentationLayout Build(AtlasProjection projection)
    {
        ArgumentNullException.ThrowIfNull(projection);

        var byId = new Dictionary<string, AtlasNodePresentationViewModel>(StringComparer.Ordinal);
        var core = projection.Nodes.Single(node => node.Kind == AtlasNodeKind.Core);
        var corePresentation = new AtlasNodePresentationViewModel(core, 0, 0);
        byId.Add(core.NodeId, corePresentation);

        var services = projection.Nodes
            .Where(node => node.Kind == AtlasNodeKind.Service)
            .OrderBy(ServiceLayoutOrder)
            .ThenBy(node => node.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(node => node.NodeId, StringComparer.Ordinal)
            .ToArray();
        corePresentation.ChildNodeCount = projection.Connections.Count(connection =>
            connection.Kind == AtlasConnectionKind.Composition
            && string.Equals(connection.SourceNodeId, core.NodeId, StringComparison.Ordinal));

        for (var index = 0; index < services.Length; index++)
        {
            var service = services[index];
            var placement = ServicePlacement(service, index, services.Length);
            var serviceX = placement.X;
            var serviceY = placement.Y;
            var serviceAngle = placement.Angle;

            var capabilities = projection.Nodes
                .Where(node => node.Kind == AtlasNodeKind.Capability && node.ServiceIdentity == service.ServiceIdentity)
                .OrderBy(node => node.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(node => node.NodeId, StringComparer.Ordinal)
                .ToArray();

            var servicePresentation = new AtlasNodePresentationViewModel(service, serviceX, serviceY)
            {
                ChildNodeCount = capabilities.Length
            };
            byId.Add(service.NodeId, servicePresentation);

            // Capability presentation is local to the owning provider and does not make each
            // published/integration contract a first-level global Atlas destination.
            const double capabilityRadius = 166;
            const double spread = Math.PI * 0.42;
            for (var capabilityIndex = 0; capabilityIndex < capabilities.Length; capabilityIndex++)
            {
                var offset = capabilities.Length == 1
                    ? 0
                    : -spread / 2 + spread * capabilityIndex / (capabilities.Length - 1);
                var capabilityAngle = serviceAngle + offset;
                var capability = capabilities[capabilityIndex];
                byId.Add(capability.NodeId, new AtlasNodePresentationViewModel(
                    capability,
                    serviceX + Math.Cos(capabilityAngle) * capabilityRadius,
                    serviceY + Math.Sin(capabilityAngle) * capabilityRadius));
            }
        }

        var nodes = projection.Nodes
            .Where(node => byId.ContainsKey(node.NodeId))
            .Select(node => byId[node.NodeId])
            .ToArray();
        var connections = projection.Connections
            .Where(connection => byId.ContainsKey(connection.SourceNodeId) && byId.ContainsKey(connection.TargetNodeId))
            .Select(connection => new AtlasConnectionPresentationViewModel(connection, byId[connection.SourceNodeId], byId[connection.TargetNodeId]))
            .ToArray();

        foreach (var connection in connections.Where(connection =>
                     connection.Kind is AtlasConnectionKind.CapabilityConsumption or AtlasConnectionKind.CapabilityDependency))
        {
            var description = connection.Model.Description ?? "This product uses another WGT capability.";
            connection.Source.AddRelationship(new(
                "Uses",
                connection.Target.Title,
                connection.Target.NodeId,
                description));
            connection.Target.AddRelationship(new(
                "Used by",
                connection.Source.Title,
                connection.Source.NodeId,
                description));
        }

        return new AtlasPresentationLayout(nodes, connections);
    }

    private static int ServiceLayoutOrder(AtlasNode node)
    {
        var id = node.ServiceIdentity?.Value;
        return id is not null && PrimaryServiceOrder.TryGetValue(id, out var order)
            ? order
            : PrimaryServiceOrder.Count + 100;
    }

    private static (double X, double Y, double Angle) ServicePlacement(AtlasNode service, int index, int count)
    {
        return service.ServiceIdentity?.Value switch
        {
            "illumination" => (0, -255, -Math.PI / 2),
            "orientation" => (365, 0, 0),
            "conveyance" => (0, 255, Math.PI / 2),
            "vocation" => (-365, 0, Math.PI),
            _ => FallbackServicePlacement(index, count)
        };
    }

    private static (double X, double Y, double Angle) FallbackServicePlacement(int index, int count)
    {
        var angle = count <= 1
            ? -Math.PI / 2
            : -Math.PI / 2 + 2 * Math.PI * index / count;
        var radius = count switch
        {
            <= 8 => 330d,
            <= 15 => 470d,
            _ => 560d
        };
        return (Math.Cos(angle) * radius, Math.Sin(angle) * radius, angle);
    }
}
