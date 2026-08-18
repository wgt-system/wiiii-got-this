using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Presentation;

public sealed record AtlasRelationshipPresentationViewModel(
    string Direction,
    string RelatedNodeTitle,
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
    public bool IsEnabled => Model.IsEnabled;
    public bool IsAvailable => Model.IsAvailable;
    public bool IsIntegrated => Model.IsIntegrated;
    public bool IsKnownOnlyService => IsService && !IsIntegrated;
    public bool IsIntegratedService => IsService && IsIntegrated;
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
    public string KindLabel => Kind switch
    {
        AtlasNodeKind.Core => "WGT CORE",
        AtlasNodeKind.Service => "SERVICE",
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
        AtlasNodeKind.Service when IsAvailable => "READY",
        AtlasNodeKind.Service => "LOCAL",
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

        foreach (var connection in connections.Where(item => item.Kind == AtlasConnectionKind.CapabilityDependency))
        {
            if (focused.Contains(connection.Source.NodeId) || focused.Contains(connection.Target.NodeId))
            {
                focused.Add(connection.Source.NodeId);
                focused.Add(connection.Target.NodeId);
            }
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
        byId.Add(core.NodeId, new AtlasNodePresentationViewModel(core, 0, 0));

        var services = projection.Nodes
            .Where(node => node.Kind == AtlasNodeKind.Service)
            .OrderBy(ServiceLayoutOrder)
            .ThenBy(node => node.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(node => node.NodeId, StringComparer.Ordinal)
            .ToArray();

        const double serviceRadius = 350;
        for (var index = 0; index < services.Length; index++)
        {
            var angle = ServiceAngle(index, services.Length);
            var serviceX = Math.Cos(angle) * serviceRadius;
            var serviceY = Math.Sin(angle) * serviceRadius;
            var service = services[index];
            byId.Add(service.NodeId, new AtlasNodePresentationViewModel(service, serviceX, serviceY));

            var capabilities = projection.Nodes
                .Where(node => node.Kind == AtlasNodeKind.Capability && node.ServiceIdentity == service.ServiceIdentity)
                .OrderBy(node => node.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(node => node.NodeId, StringComparer.Ordinal)
                .ToArray();

            const double capabilityRadius = 152;
            const double spread = Math.PI * 0.5;
            for (var capabilityIndex = 0; capabilityIndex < capabilities.Length; capabilityIndex++)
            {
                var offset = capabilities.Length == 1
                    ? 0
                    : -spread / 2 + spread * capabilityIndex / (capabilities.Length - 1);
                var capabilityAngle = angle + offset;
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

        foreach (var connection in connections.Where(connection => connection.Kind == AtlasConnectionKind.CapabilityDependency))
        {
            var description = connection.Model.Description ?? "This capability depends on another WGT service.";
            connection.Source.AddRelationship(new("Uses", connection.Target.Title, description));
            connection.Target.AddRelationship(new("Used by", connection.Source.Title, description));
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

    private static double ServiceAngle(int index, int count)
    {
        if (count <= 1)
            return -Math.PI / 2;
        return -Math.PI / 2 + 2 * Math.PI * index / count;
    }
}
