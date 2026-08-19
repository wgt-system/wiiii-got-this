using Avalonia.Controls;
using Avalonia.VisualTree;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopAtlasView
{
    private bool finalInspectorSectionsPrepared;
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

        var dependencies = existing.FirstOrDefault(item =>
            string.Equals(item.Header?.ToString(), "Dependencies", StringComparison.Ordinal));
        if (dependencies is not null)
        {
            dependencies.Header = "Links";
            ToolTip.SetTip(dependencies, "Dependencies and explicit cross-service relationships");
        }

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

        var systemIndex = existing.FindIndex(item =>
            string.Equals(item.Header?.ToString(), "System", StringComparison.Ordinal));
        var insertIndex = systemIndex >= 0 ? systemIndex : tabs.Items.Count;
        tabs.Items.Insert(insertIndex, dataTab);
        tabs.Items.Insert(insertIndex + 1, deviceTab);
        finalInspectorSectionsPrepared = true;
        UpdateFinalInspectorFacts();
    }

    private void UpdateFinalInspectorFacts()
    {
        EnsureFinalInspectorSections();
        if (!finalInspectorSectionsPrepared || finalVisualShell?.SelectedAtlasNode is not { } node)
            return;

        var integration = finalVisualShell.SelectedIntegration;
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
        WiiiiGotThis.Application.AtlasNodeKind.Core =>
            "WGT owns Atlas composition and host behavior. Provider domain semantics, provider UI and provider persistence remain provider-owned.",
        WiiiiGotThis.Application.AtlasNodeKind.Service =>
            $"{node.Title} remains a provider-owned bounded context. Hosting or composing it in WGT does not transfer its domain ownership.",
        _ =>
            "This capability is provider-published. WGT renders its availability and explicit relationships without becoming the capability owner."
    };

    private static string DataBoundaryFact(AtlasNodePresentationViewModel node)
    {
        if (node.IsCore)
        {
            return "The Atlas is a WGT presentation/read model over registered integrations, enablement and published capability state; it is not a shared provider database.";
        }

        if (node.IsKnownOnlyService)
        {
            return "Only this service identity is known on the current client. No provider capability publication or provider domain data is composed here yet.";
        }

        if (node.IsService)
        {
            return "WGT keeps host-side integration, enablement and last-known publication metadata. Opening the provider product does not copy provider domain records into WGT.";
        }

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
