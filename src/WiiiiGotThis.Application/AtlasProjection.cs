using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Application;

public enum AtlasNodeKind
{
    Core,
    Service,
    Capability
}

public enum AtlasConnectionKind
{
    Composition,
    CapabilityOwnership
}

public sealed record AtlasNode(
    string NodeId,
    AtlasNodeKind Kind,
    string Title,
    string Subtitle,
    ServiceIdentity? ServiceIdentity = null,
    CapabilityIdentity? CapabilityIdentity = null,
    bool IsEnabled = true,
    bool IsAvailable = true,
    AvailabilityReason? AvailabilityReason = null);

public sealed record AtlasConnection(
    string ConnectionId,
    AtlasConnectionKind Kind,
    string SourceNodeId,
    string TargetNodeId);

public sealed record AtlasProjection(
    IReadOnlyList<AtlasNode> Nodes,
    IReadOnlyList<AtlasConnection> Connections);

public sealed class BuildAtlasProjectionUseCase
{
    public const string CoreNodeId = "wgt.core";

    public AtlasProjection Build(
        IReadOnlyCollection<ServiceIntegrationListItem> integrations,
        IReadOnlyCollection<CapabilityCatalogEntry> capabilities,
        bool includeDeveloperIntegrations = false)
    {
        ArgumentNullException.ThrowIfNull(integrations);
        ArgumentNullException.ThrowIfNull(capabilities);

        var visibleIntegrations = integrations
            .Where(integration => includeDeveloperIntegrations || !IsDeveloperIntegration(integration.ServiceIdentity))
            .OrderBy(integration => integration.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(integration => integration.ServiceIdentity.Value, StringComparer.Ordinal)
            .ToArray();

        var nodes = new List<AtlasNode>(1 + visibleIntegrations.Length + capabilities.Count)
        {
            new(
                CoreNodeId,
                AtlasNodeKind.Core,
                "Wiiii Got This",
                "Your service and capability system")
        };
        var connections = new List<AtlasConnection>();

        foreach (var integration in visibleIntegrations)
        {
            var serviceNodeId = ServiceNodeId(integration.ServiceIdentity);
            var serviceAvailable = integration.IsEffectivelyEnabled && integration.HasLastKnownPublication;
            nodes.Add(new(
                serviceNodeId,
                AtlasNodeKind.Service,
                integration.DisplayName,
                DescribeService(integration),
                integration.ServiceIdentity,
                IsEnabled: integration.IsEffectivelyEnabled,
                IsAvailable: serviceAvailable));
            connections.Add(new(
                $"composition:{integration.ServiceIdentity.Value}",
                AtlasConnectionKind.Composition,
                CoreNodeId,
                serviceNodeId));

            foreach (var capability in capabilities
                         .Where(capability => capability.ServiceIdentity == integration.ServiceIdentity)
                         .OrderBy(capability => capability.CapabilityTitle, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(capability => capability.CapabilityIdentity.Value, StringComparer.Ordinal))
            {
                var capabilityNodeId = CapabilityNodeId(capability.ServiceIdentity, capability.CapabilityIdentity);
                nodes.Add(new(
                    capabilityNodeId,
                    AtlasNodeKind.Capability,
                    capability.CapabilityTitle,
                    capability.Resolution.Availability.IsAvailable ? "Available" : "Unavailable",
                    capability.ServiceIdentity,
                    capability.CapabilityIdentity,
                    integration.IsEffectivelyEnabled,
                    capability.Resolution.Availability.IsAvailable,
                    capability.Resolution.Availability.IsAvailable ? null : capability.Resolution.Availability.Reason));
                connections.Add(new(
                    $"capability:{capability.ServiceIdentity.Value}:{capability.CapabilityIdentity.Value}",
                    AtlasConnectionKind.CapabilityOwnership,
                    serviceNodeId,
                    capabilityNodeId));
            }
        }

        return new AtlasProjection(nodes.AsReadOnly(), connections.AsReadOnly());
    }

    public static string ServiceNodeId(ServiceIdentity serviceIdentity)
    {
        ArgumentNullException.ThrowIfNull(serviceIdentity);
        return $"service:{serviceIdentity.Value}";
    }

    public static string CapabilityNodeId(ServiceIdentity serviceIdentity, CapabilityIdentity capabilityIdentity)
    {
        ArgumentNullException.ThrowIfNull(serviceIdentity);
        ArgumentNullException.ThrowIfNull(capabilityIdentity);
        return $"capability:{serviceIdentity.Value}:{capabilityIdentity.Value}";
    }

    private static bool IsDeveloperIntegration(ServiceIdentity serviceIdentity) =>
        string.Equals(serviceIdentity.Value, "reference", StringComparison.Ordinal);

    private static string DescribeService(ServiceIntegrationListItem integration)
    {
        if (!integration.IsEffectivelyEnabled)
            return "Disabled on this device";
        if (!integration.HasLastKnownPublication)
            return "Waiting for a valid service publication";
        return integration.LatestRefreshResult switch
        {
            IntegrationRefreshStatus.Refreshed => "Connected",
            IntegrationRefreshStatus.AdapterFailed => "Using last known service state",
            IntegrationRefreshStatus.InvalidPublication => "Using last valid service state",
            _ => "Known service"
        };
    }
}
