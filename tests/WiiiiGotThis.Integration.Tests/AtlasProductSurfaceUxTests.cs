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
}
