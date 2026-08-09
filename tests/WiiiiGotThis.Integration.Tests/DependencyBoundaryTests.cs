namespace WiiiiGotThis.Integration.Tests;

public sealed class DependencyBoundaryTests
{
    [Fact]
    public void Domain_is_framework_free_and_application_does_not_depend_on_outer_layers()
    {
        var domainReferences = typeof(WiiiiGotThis.Domain.ServiceId).Assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToHashSet();
        Assert.DoesNotContain("Avalonia", domainReferences);
        Assert.DoesNotContain("Microsoft.Data.Sqlite", domainReferences);
        Assert.DoesNotContain("System.Net.Http", domainReferences);

        var applicationReferences = typeof(WiiiiGotThis.Application.RefreshPublicationsUseCase).Assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToHashSet();
        Assert.DoesNotContain("WiiiiGotThis.Infrastructure", applicationReferences);
        Assert.DoesNotContain("WiiiiGotThis.Presentation", applicationReferences);
    }

    [Fact]
    public void Infrastructure_references_application_as_its_port_boundary()
    {
        var references = typeof(WiiiiGotThis.Infrastructure.MigrationRunner).Assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToHashSet();
        Assert.Contains("WiiiiGotThis.Application", references);
    }

    [Fact]
    public void Reference_adapter_is_static_and_has_no_foreign_provider_dependency()
    {
        var assembly = typeof(WiiiiGotThis.Integrations.Reference.ReferenceIntegrationAdapter).Assembly;
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), reference => reference.Name is "Vocation" or "Illumination");
    }
}
