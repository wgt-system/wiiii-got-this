namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasDossierV2ContractTests
{
    [Fact]
    public void Normal_dossier_has_no_taxonomy_badges_or_nested_metric_cards()
    {
        var root = FindRepositoryRoot();
        var xaml = File.ReadAllText(Path.Combine(root, "src", "WiiiiGotThis.Presentation", "DesktopAtlasView.axaml"));
        var styles = File.ReadAllText(Path.Combine(root, "src", "WiiiiGotThis.Presentation", "Styles", "AtlasDossierV2Styles.axaml"));
        var app = File.ReadAllText(Path.Combine(root, "src", "WiiiiGotThis.Presentation", "App.axaml"));

        var inspectorStart = xaml.IndexOf("x:Name=\"InspectorCard\"", StringComparison.Ordinal);
        var inspectorEnd = xaml.IndexOf("Classes=\"wgt-state-banner\"", inspectorStart, StringComparison.Ordinal);
        Assert.True(inspectorStart >= 0 && inspectorEnd > inspectorStart);
        var inspector = xaml[inspectorStart..inspectorEnd];

        Assert.DoesNotContain("KindLabel", inspector, StringComparison.Ordinal);
        Assert.DoesNotContain("CompactStateText", inspector, StringComparison.Ordinal);
        Assert.DoesNotContain("wgt-atlas-kind-chip", inspector, StringComparison.Ordinal);
        Assert.Contains("Grid.Row=\"4\"", inspector, StringComparison.Ordinal);
        Assert.Contains("wgt-primary-action", inspector, StringComparison.Ordinal);
        Assert.Contains("OnInspectorHeaderPointerPressed", inspector, StringComparison.Ordinal);
        Assert.Contains("OnCloseInspector", inspector, StringComparison.Ordinal);

        Assert.Contains("Height\" Value=\"560", styles, StringComparison.Ordinal);
        Assert.Contains("Border.wgt-atlas-inspector Border.wgt-atlas-kind-chip", styles, StringComparison.Ordinal);
        Assert.Contains("Border.wgt-atlas-inspector Border.wgt-atlas-state-chip", styles, StringComparison.Ordinal);
        Assert.Contains("Property=\"IsVisible\" Value=\"False\"", styles, StringComparison.Ordinal);
        Assert.Contains("Border.wgt-atlas-inspector Border.wgt-inspector-metric", styles, StringComparison.Ordinal);
        Assert.Contains("Property=\"Background\" Value=\"Transparent\"", styles, StringComparison.Ordinal);
        Assert.Contains("AtlasDossierV2Styles.axaml", app, StringComparison.Ordinal);
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

        throw new DirectoryNotFoundException("Could not locate the Wiiii Got This repository root.");
    }
}
