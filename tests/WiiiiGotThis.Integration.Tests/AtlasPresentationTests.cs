using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;
using WiiiiGotThis.Presentation;

namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasPresentationTests
{
    [Theory]
    [InlineData("vocation", "Vocation")]
    [InlineData("illumination", "Illumination")]
    [InlineData("orientation", "Orientation")]
    public void Integrated_first_class_service_can_enter_full_product_even_before_enablement(
        string serviceId,
        string title)
    {
        var model = new AtlasNode(
            $"service:{serviceId}",
            AtlasNodeKind.Service,
            title,
            "Disabled on this device",
            new ServiceIdentity(serviceId),
            IsEnabled: false,
            IsAvailable: false,
            IsIntegrated: true);

        var node = new AtlasNodePresentationViewModel(model, 0, 0);

        Assert.True(node.CanOpenProductSurface);
        Assert.Equal($"Enable & open {title}", node.OpenProductSurfaceLabel);
    }

    [Fact]
    public void Known_only_service_does_not_claim_a_product_surface()
    {
        var model = new AtlasNode(
            "service:future",
            AtlasNodeKind.Service,
            "Future",
            "Not composed on this client yet",
            new ServiceIdentity("future"),
            IsEnabled: false,
            IsAvailable: false,
            IsIntegrated: false);

        var node = new AtlasNodePresentationViewModel(model, 0, 0);

        Assert.False(node.CanOpenProductSurface);
    }
}
