using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Application;

public enum AtlasNodeKind
{
    Core,
    Service,
    Capability
}

public enum AtlasProductRole
{
    FirstClassProductProvider,
    SharedCapabilityProvider,
    DualRoleProvider
}

public enum AtlasConnectionKind
{
    Composition,
    CapabilityOwnership,
    CapabilityDependency
}

public sealed record AtlasProductService(
    ServiceIdentity ServiceIdentity,
    string DisplayName,
    string Description,
    AtlasProductRole ProductRole = AtlasProductRole.FirstClassProductProvider);

public sealed record AtlasProductDependency(
    ServiceIdentity SourceServiceIdentity,
    CapabilityIdentity SourceCapabilityIdentity,
    ServiceIdentity TargetServiceIdentity,
    string Description);

public sealed record AtlasNode(
    string NodeId,
    AtlasNodeKind Kind,
    string Title,
    string Subtitle,
    ServiceIdentity? ServiceIdentity = null,
    CapabilityIdentity? CapabilityIdentity = null,
    bool IsEnabled = true,
    bool IsAvailable = true,
    AvailabilityReason? AvailabilityReason = null,
    bool IsIntegrated = true,
    string? Description = null,
    AtlasProductRole? ProductRole = null);

public sealed record AtlasConnection(
    string ConnectionId,
    AtlasConnectionKind Kind,
    string SourceNodeId,
    string TargetNodeId,
    string? Description = null);

public sealed record AtlasProjection(
    IReadOnlyList<AtlasNode> Nodes,
    IReadOnlyList<AtlasConnection> Connections);

public sealed class BuildAtlasProjectionUseCase
{
    public const string CoreNodeId = "wgt.core";
    private const string ReferenceDeveloperServiceId = "reference-service";

    private static readonly IReadOnlyList<AtlasProductService> DefaultProductServices = Array.AsReadOnly<AtlasProductService>(
    [
        new(
            new ServiceIdentity("vocation"),
            "Vocation",
            "Personal job-market, research and application work.",
            AtlasProductRole.FirstClassProductProvider),
        new(
            new ServiceIdentity("illumination"),
            "Illumination",
            "Learning, review, study and learning insight.",
            AtlasProductRole.FirstClassProductProvider),
        new(
            new ServiceIdentity("orientation"),
            "Orientation",
            "Spatial discovery, exploration, navigation and mobility.",
            AtlasProductRole.DualRoleProvider),
        new(
            new ServiceIdentity("conveyance"),
            "Conveyance",
            "Durable opaque delivery between devices without taking ownership of provider data.",
            AtlasProductRole.SharedCapabilityProvider)
    ]);

    private static readonly IReadOnlyList<AtlasProductDependency> DefaultProductDependencies = Array.AsReadOnly<AtlasProductDependency>(
    [
        new(
            new ServiceIdentity("vocation"),
            new CapabilityIdentity("vocation.map_projection"),
            new ServiceIdentity("orientation"),
            "Vocation owns opportunity and work-location meaning; Orientation supplies the generic geospatial rendering and interaction capability.")
    ]);

    private readonly StringComparer titleComparer = StringComparer.OrdinalIgnoreCase;
    private readonly IReadOnlyList<AtlasProductService> productServices;
    private readonly IReadOnlyList<AtlasProductDependency> productDependencies;

    public BuildAtlasProjectionUseCase(
        IEnumerable<AtlasProductService>? productServices = null,
        IEnumerable<AtlasProductDependency>? productDependencies = null)
    {
        var configuredServices = (productServices ?? DefaultProductServices).ToArray();
        var identities = new HashSet<ServiceIdentity>();
        foreach (var service in configuredServices)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentException.ThrowIfNullOrWhiteSpace(service.DisplayName);
            ArgumentException.ThrowIfNullOrWhiteSpace(service.Description);
            if (!identities.Add(service.ServiceIdentity))
                throw new ArgumentException($"Duplicate Atlas product Service '{service.ServiceIdentity.Value}'.", nameof(productServices));
        }
        this.productServices = Array.AsReadOnly(configuredServices);

        var configuredDependencies = (productDependencies ?? DefaultProductDependencies).ToArray();
        foreach (var dependency in configuredDependencies)
        {
            ArgumentNullException.ThrowIfNull(dependency);
            ArgumentException.ThrowIfNullOrWhiteSpace(dependency.Description);
        }
        this.productDependencies = Array.AsReadOnly(configuredDependencies);
    }

    public AtlasProjection Build(
        IReadOnlyCollection<ServiceIntegrationListItem> integrations,
        IReadOnlyCollection<CapabilityCatalogEntry> capabilities,
        bool includeDeveloperIntegrations = false)
    {
        ArgumentNullException.ThrowIfNull(integrations);
        ArgumentNullException.ThrowIfNull(capabilities);

        var visibleIntegrations = integrations
            .Where(integration => includeDeveloperIntegrations || !IsDeveloperIntegration(integration.ServiceIdentity))
            .ToDictionary(integration => integration.ServiceIdentity);
        var knownProducts = productServices.ToDictionary(service => service.ServiceIdentity);

        var serviceEntries = visibleIntegrations.Keys
            .Concat(knownProducts.Keys)
            .Distinct()
            .Select(identity =>
            {
                visibleIntegrations.TryGetValue(identity, out var integration);
                knownProducts.TryGetValue(identity, out var product);
                return new
                {
                    Identity = identity,
                    Integration = integration,
                    Product = product,
                    Title = product?.DisplayName ?? integration?.DisplayName ?? identity.Value
                };
            })
            .OrderBy(entry => entry.Title, titleComparer)
            .ThenBy(entry => entry.Identity.Value, StringComparer.Ordinal)
            .ToArray();

        var nodes = new List<AtlasNode>(1 + serviceEntries.Length + capabilities.Count)
        {
            new(
                CoreNodeId,
                AtlasNodeKind.Core,
                "Wiiii Got This",
                "System ready",
                Description: "Your service and capability system.")
        };
        var connections = new List<AtlasConnection>();

        foreach (var entry in serviceEntries)
        {
            var integration = entry.Integration;
            var integrated = integration is not null;
            var serviceAvailable = integrated && integration!.IsEffectivelyEnabled && integration.HasLastKnownPublication;
            var serviceNodeId = ServiceNodeId(entry.Identity);
            nodes.Add(new(
                serviceNodeId,
                AtlasNodeKind.Service,
                entry.Title,
                integrated ? DescribeService(integration!) : "Not composed on this client yet",
                entry.Identity,
                IsEnabled: integrated && integration!.IsEffectivelyEnabled,
                IsAvailable: serviceAvailable,
                IsIntegrated: integrated,
                Description: entry.Product?.Description,
                ProductRole: entry.Product?.ProductRole));

            // Product composition is not the same thing as service/runtime existence.
            // Known shared infrastructure such as Conveyance stays discoverable in Atlas
            // without being projected as a peer end-user product destination.
            if (entry.Product?.ProductRole != AtlasProductRole.SharedCapabilityProvider)
            {
                connections.Add(new(
                    $"composition:{entry.Identity.Value}",
                    AtlasConnectionKind.Composition,
                    CoreNodeId,
                    serviceNodeId));
            }

            if (!integrated)
                continue;

            foreach (var capability in capabilities
                         .Where(capability => capability.ServiceIdentity == entry.Identity)
                         .OrderBy(capability => capability.CapabilityTitle, titleComparer)
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
                    integration!.IsEffectivelyEnabled,
                    capability.Resolution.Availability.IsAvailable,
                    capability.Resolution.Availability.IsAvailable ? null : capability.Resolution.Availability.Reason,
                    ProductRole: entry.Product?.ProductRole));
                connections.Add(new(
                    $"capability:{capability.ServiceIdentity.Value}:{capability.CapabilityIdentity.Value}",
                    AtlasConnectionKind.CapabilityOwnership,
                    serviceNodeId,
                    capabilityNodeId));
            }
        }

        var existingNodeIds = nodes.Select(node => node.NodeId).ToHashSet(StringComparer.Ordinal);
        foreach (var dependency in productDependencies)
        {
            var sourceNodeId = CapabilityNodeId(dependency.SourceServiceIdentity, dependency.SourceCapabilityIdentity);
            var targetNodeId = ServiceNodeId(dependency.TargetServiceIdentity);
            if (!existingNodeIds.Contains(sourceNodeId) || !existingNodeIds.Contains(targetNodeId))
                continue;

            connections.Add(new(
                $"dependency:{dependency.SourceServiceIdentity.Value}:{dependency.SourceCapabilityIdentity.Value}:{dependency.TargetServiceIdentity.Value}",
                AtlasConnectionKind.CapabilityDependency,
                sourceNodeId,
                targetNodeId,
                dependency.Description));
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
        string.Equals(serviceIdentity.Value, ReferenceDeveloperServiceId, StringComparison.Ordinal);

    private static string DescribeService(ServiceIntegrationListItem integration)
    {
        if (!integration.IsEffectivelyEnabled)
            return "Disabled on this device";
        if (!integration.HasLastKnownPublication)
            return integration.HasRefreshBeenAttempted
                ? "No valid service publication available"
                : "Waiting for a valid service publication";
        return integration.LatestRefreshResult switch
        {
            IntegrationRefreshStatus.Refreshed => "Connected",
            IntegrationRefreshStatus.AdapterFailed => "Using last known service state",
            IntegrationRefreshStatus.InvalidPublication => "Using last valid service state",
            _ => "Known service"
        };
    }
}
