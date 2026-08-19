using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.VisualTree;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopAtlasView
{
    private bool finalInspectorSectionsPrepared;
    private Border? activationPreview;
    private TextBlock? activationCapabilitiesFact;
    private TextBlock? activationDependenciesFact;
    private TextBlock? activationDataFact;
    private TextBlock? activationHostFact;
    private TextBlock? activationDeviceFact;
    private TextBlock? activationUnknownsFact;
    private TextBlock? inspectorOwnershipFact;
    private TextBlock? inspectorDataBoundaryFact;
    private TextBlock? inspectorTransportFact;
    private TextBlock? inspectorDeviceNameFact;
    private TextBlock? inspectorAvailabilityFact;
    private TextBlock? inspectorEnablementFact;
    private TextBlock? inspectorConnectionFact;

    private void EnsureFinalInspectorSections()
    {
        if (finalInspectorSectionsPrepared)
            return;

        var tabs = InspectorCard.GetVisualDescendants().OfType<TabControl>().FirstOrDefault();
        if (tabs is null)
            return;

        var existing = tabs.Items.OfType<TabItem>().ToList();
        if (existing.Any(item => string.Equals(item.Header?.ToString(), "Data", StringComparison.Ordinal)))
        {
            finalInspectorSectionsPrepared = true;
            return;
        }

        var overview = existing.FirstOrDefault(item =>
            string.Equals(item.Header?.ToString(), "Overview", StringComparison.Ordinal));
        var capabilities = existing.FirstOrDefault(item =>
            string.Equals(item.Header?.ToString(), "Capabilities", StringComparison.Ordinal));
        var dependencies = existing.FirstOrDefault(item =>
            string.Equals(item.Header?.ToString(), "Dependencies", StringComparison.Ordinal));
        var system = existing.FirstOrDefault(item =>
            string.Equals(item.Header?.ToString(), "System", StringComparison.Ordinal));

        if (overview?.Content is ScrollViewer { Content: StackPanel overviewStack })
            InsertActivationPreview(overviewStack);

        RenameInspectorTab(overview, "Node", "Overview and actions");
        RenameInspectorTab(capabilities, "Caps", "Published capabilities");
        RenameInspectorTab(dependencies, "Links", "Dependencies and explicit cross-service relationships");
        RenameInspectorTab(system, "Diag", "System and diagnostics");

        inspectorOwnershipFact = CreateInspectorFactText();
        inspectorDataBoundaryFact = CreateInspectorFactText();
        inspectorTransportFact = CreateInspectorFactText();
        inspectorDeviceNameFact = CreateInspectorFactText();
        inspectorAvailabilityFact = CreateInspectorFactText();
        inspectorEnablementFact = CreateInspectorFactText();
        inspectorConnectionFact = CreateInspectorFactText();

        var dataTab = new TabItem
        {
            Header = "Data",
            Content = BuildInspectorFactSection(
                ("OWNERSHIP", inspectorOwnershipFact),
                ("DATA BOUNDARY", inspectorDataBoundaryFact),
                ("TRANSPORT / NETWORK", inspectorTransportFact))
        };
        ToolTip.SetTip(dataTab, "Privacy & Data");

        var deviceTab = new TabItem
        {
            Header = "Device",
            Content = BuildInspectorFactSection(
                ("THIS DEVICE", inspectorDeviceNameFact),
                ("AVAILABILITY", inspectorAvailabilityFact),
                ("ENABLEMENT", inspectorEnablementFact),
                ("CONNECTION", inspectorConnectionFact))
        };
        ToolTip.SetTip(deviceTab, "Devices / Availability");

        var systemIndex = system is null ? -1 : existing.IndexOf(system);
        var insertIndex = systemIndex >= 0 ? systemIndex : tabs.Items.Count;
        tabs.Items.Insert(insertIndex, dataTab);
        tabs.Items.Insert(insertIndex + 1, deviceTab);
        finalInspectorSectionsPrepared = true;
        UpdateFinalInspectorFacts();
    }

    private static void RenameInspectorTab(TabItem? tab, string compactHeader, string tooltip)
    {
        if (tab is null)
            return;

        tab.Header = compactHeader;
        ToolTip.SetTip(tab, tooltip);
        AutomationProperties.SetName(tab, tooltip);
    }

    private void InsertActivationPreview(StackPanel overviewStack)
    {
        if (activationPreview is not null)
            return;

        activationCapabilitiesFact = CreateInspectorFactText();
        activationDependenciesFact = CreateInspectorFactText();
        activationDataFact = CreateInspectorFactText();
        activationHostFact = CreateInspectorFactText();
        activationDeviceFact = CreateInspectorFactText();
        activationUnknownsFact = CreateInspectorFactText();

        var facts = new StackPanel
        {
            Spacing = 5,
            Children =
            {
                BuildActivationScopeFact(activationCapabilitiesFact, activationDependenciesFact),
                BuildActivationFact("DATA BOUNDARY", activationDataFact),
                BuildActivationFact("HOST / NETWORK", activationHostFact),
                BuildActivationFact("THIS DEVICE", activationDeviceFact),
                BuildActivationFact("NOT PUBLISHED", activationUnknownsFact)
            }
        };

        var heading = new TextBlock
        {
            Text = "BEFORE ACTIVATION",
            FontSize = 9,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            LetterSpacing = 1.2
        };
        heading.Classes.Add("wgt-caption");

        activationPreview = new Border
        {
            Padding = new Avalonia.Thickness(10),
            IsVisible = false,
            Child = new StackPanel
            {
                Spacing = 7,
                Children = { heading, facts }
            }
        };
        activationPreview.Classes.Add("wgt-activation-preview");
        AutomationProperties.SetName(activationPreview, "Before activation");

        overviewStack.Children.Insert(Math.Min(2, overviewStack.Children.Count), activationPreview);
    }

    private static Border BuildActivationScopeFact(TextBlock capabilities, TextBlock dependencies)
    {
        var label = new TextBlock { Text = "CAPABILITIES / LINKS" };
        label.Classes.Add("wgt-caption");
        var content = new StackPanel
        {
            Spacing = 1,
            Children = { label, capabilities, dependencies }
        };
        var card = new Border
        {
            Padding = new Avalonia.Thickness(8, 6),
            Child = content
        };
        card.Classes.Add("wgt-activation-fact");
        return card;
    }

    private static Border BuildActivationFact(string labelText, TextBlock value)
    {
        var label = new TextBlock { Text = labelText };
        label.Classes.Add("wgt-caption");
        var content = new StackPanel
        {
            Spacing = 2,
            Children = { label, value }
        };
        var card = new Border
        {
            Padding = new Avalonia.Thickness(8, 6),
            Child = content
        };
        card.Classes.Add("wgt-activation-fact");
        return card;
    }

    private void UpdateFinalInspectorFacts()
    {
        EnsureFinalInspectorSections();
        if (!finalInspectorSectionsPrepared || finalVisualShell?.SelectedAtlasNode is not { } node)
            return;

        var integration = finalVisualShell.SelectedIntegration;
        UpdateActivationPreview(node, integration);
        SetFact(inspectorOwnershipFact, OwnershipFact(node));
        SetFact(inspectorDataBoundaryFact, DataBoundaryFact(node));
        SetFact(inspectorTransportFact, TransportBoundaryFact(node));
        SetFact(inspectorDeviceNameFact, finalVisualShell.CurrentDeviceName);
        SetFact(inspectorAvailabilityFact, AvailabilityFact(node));
        SetFact(
            inspectorEnablementFact,
            integration is null
                ? "No service-level enablement setting applies to this Atlas node."
                : $"{integration.GlobalEnablementText}. {integration.DeviceBehaviorText}");
        SetFact(
            inspectorConnectionFact,
            integration is null
                ? "No provider publication connection is attached to this Atlas node."
                : $"{integration.ConnectionHealthTitle}. {integration.ConnectionHealthDescription}");
    }

    private void UpdateActivationPreview(
        AtlasNodePresentationViewModel node,
        ServiceIntegrationPresentationViewModel? integration)
    {
        if (activationPreview is null || finalVisualShell is null)
            return;

        var show = node.IsIntegratedService && !node.IsEnabled && node.CanOpenProductSurface && integration is not null;
        activationPreview.IsVisible = show;
        if (!show || integration is null)
            return;

        var capabilityCount = finalVisualShell.SelectedIntegrationCapabilities.Count;
        var dependencyCount = CountExplicitServiceDependencies(node);
        SetFact(
            activationCapabilitiesFact,
            capabilityCount switch
            {
                0 => "No capability currently resolved.",
                1 => "1 provider capability currently resolved.",
                _ => $"{capabilityCount} provider capabilities currently resolved."
            });
        SetFact(
            activationDependenciesFact,
            dependencyCount switch
            {
                0 => "No explicit cross-service link published.",
                1 => "1 explicit cross-service link published.",
                _ => $"{dependencyCount} explicit cross-service links published."
            });
        SetFact(
            activationDataFact,
            "WGT changes host-side integration state only; provider domain records remain provider-owned.");
        SetFact(activationHostFact, ActivationHostSummary(node));
        SetFact(
            activationDeviceFact,
            $"Affects {finalVisualShell.CurrentDeviceName}. {integration.DeviceBehaviorText}");
        SetFact(
            activationUnknownsFact,
            "No additional permission requirement or cross-device guarantee is published to WGT.");
    }

    private static string ActivationHostSummary(AtlasNodePresentationViewModel node) => node.ServiceIdentity?.Value switch
    {
        "vocation" => "Windows host: local loopback Vocation product endpoint.",
        "orientation" => "Windows host: local loopback Orientation product endpoint.",
        "illumination" => "Windows host: provider-owned in-process Product Surface.",
        _ => "No concrete product-host guarantee is published to WGT."
    };

    private int CountExplicitServiceDependencies(AtlasNodePresentationViewModel serviceNode)
    {
        if (finalVisualShell is null || serviceNode.ServiceIdentity is not { } serviceIdentity)
            return 0;

        var ownedNodeIds = finalVisualShell.AtlasNodes
            .Where(candidate => candidate.ServiceIdentity == serviceIdentity)
            .Select(candidate => candidate.NodeId)
            .ToHashSet(StringComparer.Ordinal);

        return finalVisualShell.AtlasConnections.Count(connection =>
            connection.Kind == AtlasConnectionKind.CapabilityDependency
            && (ownedNodeIds.Contains(connection.Source.NodeId) || ownedNodeIds.Contains(connection.Target.NodeId)));
    }

    private static ScrollViewer BuildInspectorFactSection(params (string Label, TextBlock Value)[] facts)
    {
        var stack = new StackPanel
        {
            Margin = new Avalonia.Thickness(0, 12, 0, 0),
            Spacing = 8
        };

        foreach (var fact in facts)
        {
            var label = new TextBlock { Text = fact.Label };
            label.Classes.Add("wgt-caption");
            var content = new StackPanel
            {
                Spacing = 3,
                Children = { label, fact.Value }
            };
            var card = new Border
            {
                Padding = new Avalonia.Thickness(10, 8),
                Child = content
            };
            card.Classes.Add("wgt-inspector-fact");
            stack.Children.Add(card);
        }

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = stack
        };
    }

    private static TextBlock CreateInspectorFactText()
    {
        var text = new TextBlock
        {
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };
        text.Classes.Add("wgt-secondary");
        return text;
    }

    private static void SetFact(TextBlock? target, string value)
    {
        if (target is not null)
            target.Text = value;
    }

    private static string OwnershipFact(AtlasNodePresentationViewModel node) => node.Kind switch
    {
        AtlasNodeKind.Core =>
            "WGT owns Atlas composition and host behavior. Provider domain semantics, provider UI and provider persistence remain provider-owned.",
        AtlasNodeKind.Service =>
            $"{node.Title} remains a provider-owned bounded context. Hosting or composing it in WGT does not transfer its domain ownership.",
        _ =>
            "This capability is provider-published. WGT renders its availability and explicit relationships without becoming the capability owner."
    };

    private static string DataBoundaryFact(AtlasNodePresentationViewModel node)
    {
        if (node.IsCore)
            return "The Atlas is a WGT presentation/read model over registered integrations, enablement and published capability state; it is not a shared provider database.";
        if (node.IsKnownOnlyService)
            return "Only this service identity is known on the current client. No provider capability publication or provider domain data is composed here yet.";
        if (node.IsService)
            return "WGT keeps host-side integration, enablement and last-known publication metadata. Opening the provider product does not copy provider domain records into WGT.";
        return "WGT uses the provider-published/resolved capability state needed for this integration surface. No additional provider data guarantees are inferred.";
    }

    private static string TransportBoundaryFact(AtlasNodePresentationViewModel node)
    {
        var serviceId = node.ServiceIdentity?.Value;
        return serviceId switch
        {
            "vocation" =>
                "On Windows, WGT hosts Vocation through its configured local loopback product endpoint. Provider-internal network and data behavior remains Vocation-owned.",
            "orientation" =>
                "On Windows, WGT hosts Orientation through its configured local loopback product endpoint. Orientation remains authoritative for its backend, maps and routing/network behavior.",
            "illumination" =>
                "On Windows, Illumination is hosted in-process through its provider-owned Product Surface. Illumination retains its own persistence and domain composition.",
            "conveyance" =>
                "No Conveyance Product Surface or published capability runtime is composed on this client yet.",
            _ when node.IsCore =>
                "Transport remains concrete-provider-specific. WGT does not impose a universal plugin transport or shared provider runtime protocol.",
            _ =>
                "No additional transport guarantee is published to this Atlas presentation."
        };
    }

    private static string AvailabilityFact(AtlasNodePresentationViewModel node)
    {
        if (node.IsCore)
            return "The Desktop Atlas host is active on this device.";
        if (node.IsKnownOnlyService)
            return "Known to WGT, but no provider surface or capability publication is composed on this client yet.";
        if (!node.IsEnabled)
            return "Integrated with WGT but disabled on this device.";
        if (node.IsAvailable)
            return node.IsCapability
                ? "This published capability is currently available on this device."
                : "This service is composed and currently available to WGT on this device.";
        return node.IsCapability
            ? $"This capability is not currently available here. {node.AvailabilityText}"
            : $"The service is composed but not currently healthy/available here. {node.AvailabilityText}";
    }
}
