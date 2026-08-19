namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasWorldInfrastructureOverlayContractTests
{
    [Fact]
    public void World_projects_shared_capability_consumption_as_a_local_facility_and_backbone()
    {
        var root = FindRepositoryRoot();
        var overlay = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasWorldInfrastructureOverlay.cs"));
        var host = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "DesktopAtlasView.ProductionRenderer.cs"));

        Assert.Contains("provider?.IsSharedCapabilityProvider != true", overlay, StringComparison.Ordinal);
        Assert.Contains("LocalFacilityPosition", overlay, StringComparison.Ordinal);
        Assert.Contains("SharedBackbonePosition", overlay, StringComparison.Ordinal);
        Assert.Contains("SYNC RELAY", overlay, StringComparison.Ordinal);
        Assert.Contains("DrawLocalFacility", overlay, StringComparison.Ordinal);
        Assert.Contains("DrawInfrastructureRoute", overlay, StringComparison.Ordinal);
        Assert.Contains("new AtlasWorldInfrastructureOverlay", host, StringComparison.Ordinal);
        Assert.Contains("livingWorldInfrastructureOverlay.IsVisible = isLivingWorld", host, StringComparison.Ordinal);
    }

    [Fact]
    public void Local_facility_is_presentation_only_and_does_not_claim_provider_ownership()
    {
        var root = FindRepositoryRoot();
        var overlay = File.ReadAllText(Path.Combine(
            root,
            "src",
            "WiiiiGotThis.Presentation",
            "AtlasWorldInfrastructureOverlay.cs"));

        Assert.Contains("does not move capability ownership into the consuming product", overlay, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("this is the product's attachment", overlay, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not another copy of the provider/runtime", overlay, StringComparison.OrdinalIgnoreCase);
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
