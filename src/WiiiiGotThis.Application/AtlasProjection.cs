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
    CapabilityConsumption,
    CapabilityDependency
}

public sealed record AtlasProductService(
    ServiceIdentity ServiceIdentity,
    string DisplayName,
    string Description,
    AtlasProductRole ProductRole = AtlasProductRole.FirstClassProductProvider);

/// <summary>
/// Declares a user-meaningful capability consumption relationship for Atlas projection.
/// The provider owns the capability; the consumer owns its domain-specific use of it.
/// This is deliberately separate from narrow integration contracts that may transport data
/// without becoming durable user-facing Atlas capabilities.
/// </summary>
public sealed record AtlasProductDependency(
    ServiceIdentity ConsumerServiceIdentity,
    CapabilityIdentity ProviderCapabilityIdentity,
    ServiceIdentity ProviderServiceIdentity,
    string Description,
    string? CapabilityDisplayName = null);

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
    public const string OrientationGeospatialCapabilityId = "orientation.generic_geospatial";
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
            new CapabilityIdentity(OrientationGeospatialCapabilityId),
            new ServiceIdentity("orientation"),
            "Vocation owns opportunity and work-location meaning while Orientation supplies generic geospatial rendering, exploration and interaction.",
            "Generic geospatial")
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
                Description: "Your product and capability system.")
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

            if (entry.Product?.ProductRole != AtlasProductRole.SharedCapabilityProvider)
            {
                connections.Add(new(
                    $"composition:{entry.Identity.Value}",
                    AtlasConnectionKind.Composition,
                    CoreNodeId,
                    serviceNodeId));
            }

            // Known products do not automatically mirror every published adapter contract
            // into Atlas. Those contracts remain available to integration code; Atlas only
            // projects curated user-meaningful system capabilities. This keeps Vocation's
            // transitional Opportunity Overview / Map Projection contracts from becoming
            // fake global destinations merely because they are published.
            if (!integrated || entry.Product is not null)
                continue;

            AddPublishedIntegrationCapabilities(nodes, connections, capabilities, integration!, entry.Identity, serviceNodeId);
        }

        AddDeclaredProductCapabilities(nodes, connections);
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

    private static void AddPublishedIntegrationCapabilities(
        List<AtlasNode> nodes,
        List<AtlasConnection> connections,
        IReadOnlyCollection<CapabilityCatalogEntry> capabilities,
        ServiceIntegrationListItem integration,
        ServiceIdentity serviceIdentity,
        string serviceNodeId)
    {
        foreach (var capability in capabilities
                     .Where(capability => capability.ServiceIdentity == serviceIdentity)
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

    private void AddDeclaredProductCapabilities(List<AtlasNode> nodes, List<AtlasConnection> connections)
    {
        var serviceNodes = nodes
            .Where(node => node.Kind == AtlasNodeKind.Service && node.ServiceIdentity is not null)
            .ToDictionary(node => node.ServiceIdentity!, node => node);
        var existingNodeIds = nodes.Select(node => node.NodeId).ToHashSet(StringComparer.Ordinal);

        foreach (var dependency in productDependencies)
        {
            if (!serviceNodes.TryGetValue(dependency.ConsumerServiceIdentity, out var consumer)
                || !serviceNodes.TryGetValue(dependency.ProviderServiceIdentity, out var provider))
            {
                continue;
            }

            var capabilityNodeId = CapabilityNodeId(dependency.ProviderServiceIdentity, dependency.ProviderCapabilityIdentity);
            if (existingNodeIds.Add(capabilityNodeId))
            {
                nodes.Add(new(
                    capabilityNodeId,
                    AtlasNodeKind.Capability,
                    dependency.CapabilityDisplayName ?? dependency.ProviderCapabilityIdentity.Value,
                    provider.IsAvailable ? "Available" : "Unavailable",
                    dependency.ProviderServiceIdentity,
                    dependency.ProviderCapabilityIdentity,
                    provider.IsEnabled,
                    provider.IsAvailable,
                    provider.AvailabilityReason,
                    provider.IsIntegrated,
                    Description: dependency.Description,
                    ProductRole: provider.ProductRole));
                connections.Add(new(
                    $"ownership:{dependency.ProviderServiceIdentity.Value}:{dependency.ProviderCapabilityIdentity.Value}",
                    AtlasConnectionKind.CapabilityOwnership,
                    provider.NodeId,
                    capabilityNodeId,
                    $"{provider.Title} owns this generic capability."));
            }

            // CapabilityDependency is retained as the render-neutral relationship kind for
            // the current renderer stack. Its direction now correctly reads consumer product
            // -> provider-owned capability rather than Vocation-capability -> provider service.
            connections.Add(new(
                $"dependency:{dependency.ConsumerServiceIdentity.Value}:{dependency.ProviderServiceIdentity.Value}:{dependency.ProviderCapabilityIdentity.Value}",
                AtlasConnectionKind.CapabilityDependency,
                consumer.NodeId,
                capabilityNodeId,
                dependency.Description));
        }
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
