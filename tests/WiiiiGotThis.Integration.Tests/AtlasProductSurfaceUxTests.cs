using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;
using WiiiiGotThis.Presentation;

namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasProductSurfaceUxTests
{
    [Theory]
    [InlineData("vocation", "Vocation")]
    [InlineData("illumination", "Illumination")]
    [InlineData("orientation", "Orientation")]
    public void Integrated_available_rich_services_expose_full_product_entry(string serviceId, string title)
    {
        var node = new AtlasNode(
            $"service:{serviceId}",
            AtlasNodeKind.Service,
            title,
            "Available",
            new ServiceIdentity(serviceId),
            IsEnabled: true,
            IsAvailable: true,
            IsIntegrated: true);

        var presentation = new AtlasNodePresentationViewModel(node, 0, 0);

        Assert.True(presentation.CanOpenProductSurface);
        Assert.Equal($"Open {title}", presentation.OpenProductSurfaceLabel);
    }

    [Fact]
    public void Shared_presentation_does_not_reference_Illumination_provider_assemblies()
    {
        var references = typeof(DesktopAtlasView).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(references, name => name.StartsWith("Illumination.", StringComparison.Ordinal));
    }
}
