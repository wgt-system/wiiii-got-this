namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasInspectorFactsContractTests
{
    [Fact]
    public void Inspector_exposes_privacy_data_and_device_availability_as_first_class_sections()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.InspectorFacts.cs"));

        Assert.Contains("Header = \"Data\"", source, StringComparison.Ordinal);
        Assert.Contains("Privacy & Data", source, StringComparison.Ordinal);
        Assert.Contains("Header = \"Device\"", source, StringComparison.Ordinal);
        Assert.Contains("Devices / Availability", source, StringComparison.Ordinal);
        Assert.Contains("OWNERSHIP", source, StringComparison.Ordinal);
        Assert.Contains("DATA BOUNDARY", source, StringComparison.Ordinal);
        Assert.Contains("TRANSPORT / NETWORK", source, StringComparison.Ordinal);
        Assert.Contains("AVAILABILITY", source, StringComparison.Ordinal);
        Assert.Contains("ENABLEMENT", source, StringComparison.Ordinal);
        Assert.Contains("CONNECTION", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Inspector_facts_preserve_provider_ownership_and_concrete_host_boundaries()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.InspectorFacts.cs"));

        Assert.Contains("provider-owned bounded context", source, StringComparison.Ordinal);
        Assert.Contains("does not copy provider domain records into WGT", source, StringComparison.Ordinal);
        Assert.Contains("configured local loopback product endpoint", source, StringComparison.Ordinal);
        Assert.Contains("hosted in-process through its provider-owned Product Surface", source, StringComparison.Ordinal);
        Assert.Contains("does not impose a universal plugin transport", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Inspector_facts_refresh_on_selection_integration_and_device_changes()
    {
        var root = FindRepositoryRoot();
        var polish = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.Polish.cs"));

        Assert.Contains("EnsureFinalInspectorSections();", polish, StringComparison.Ordinal);
        Assert.Contains("UpdateFinalInspectorFacts();", polish, StringComparison.Ordinal);
        Assert.Contains("nameof(ShellViewModel.SelectedIntegration)", polish, StringComparison.Ordinal);
        Assert.Contains("nameof(ShellViewModel.CurrentDeviceName)", polish, StringComparison.Ordinal);
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
