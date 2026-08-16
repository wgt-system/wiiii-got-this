using System.Text.Json;
using WiiiiGotThis.Application;
using WiiiiGotThis.Presentation;

namespace WiiiiGotThis.Integration.Tests;

public sealed class OrientationMapBridgeAdapterTests
{
    [Fact]
    public void Scene_replace_preserves_vocation_identity_semantics_in_generic_orientation_shape()
    {
        var feature = new VocationMapFeaturePresentationViewModel(new VocationMapFeature(
            "feature-1",
            "opportunity-1",
            "Backend Engineer",
            new VocationMapCompany("company-1", "Acme GmbH"),
            new VocationMapWorkLocation("Hamburg", "city"),
            new VocationMapCoordinates(53.55, 10.0)));

        var json = OrientationMapBridgeAdapter.CreateSceneReplaceMessage([feature]);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("orientation.host-bridge", root.GetProperty("contract").GetString());
        Assert.Equal("1.0", root.GetProperty("version").GetString());
        Assert.Equal("scene.replace", root.GetProperty("type").GetString());

        var payload = root.GetProperty("payload");
        var sceneFeature = payload.GetProperty("features")[0];
        Assert.Equal("feature-1", sceneFeature.GetProperty("ref").GetString());
        Assert.Equal("vocation.map_projection", sceneFeature.GetProperty("sourceRef").GetString());
        Assert.Equal("Backend Engineer", sceneFeature.GetProperty("title").GetString());
        Assert.Equal("Acme GmbH · Hamburg", sceneFeature.GetProperty("subtitle").GetString());
        Assert.Equal(10.0, sceneFeature.GetProperty("coordinate").GetProperty("longitude").GetDouble());
        Assert.Equal(53.55, sceneFeature.GetProperty("coordinate").GetProperty("latitude").GetDouble());

        var rows = sceneFeature.GetProperty("information")[0].GetProperty("rows");
        Assert.Equal("Company", rows[0].GetProperty("label").GetString());
        Assert.Equal("Acme GmbH", rows[0].GetProperty("value").GetString());
        Assert.Equal("Location", rows[1].GetProperty("label").GetString());
        Assert.Equal("Hamburg", rows[1].GetProperty("value").GetString());
        Assert.Equal("Precision", rows[2].GetProperty("label").GetString());
        Assert.Equal("city", rows[2].GetProperty("value").GetString());

        var viewport = payload.GetProperty("viewport");
        Assert.Equal("automatic", viewport.GetProperty("kind").GetString());
        Assert.Equal(48, viewport.GetProperty("padding").GetInt32());
        Assert.Equal(15, viewport.GetProperty("maxZoom").GetInt32());
    }

    [Fact]
    public void Outbound_parser_accepts_only_the_frozen_orientation_bridge_envelope()
    {
        const string valid = "{\"contract\":\"orientation.host-bridge\",\"version\":\"1.0\",\"type\":\"bridge.ready\",\"payload\":{}}";
        Assert.True(OrientationMapBridgeAdapter.TryParseOutboundMessage(valid, out var message));
        Assert.NotNull(message);
        Assert.Equal("bridge.ready", message.Type);

        Assert.False(OrientationMapBridgeAdapter.TryParseOutboundMessage(null, out _));
        Assert.False(OrientationMapBridgeAdapter.TryParseOutboundMessage("not-json", out _));
        Assert.False(OrientationMapBridgeAdapter.TryParseOutboundMessage(valid.Replace("orientation.host-bridge", "other", StringComparison.Ordinal), out _));
        Assert.False(OrientationMapBridgeAdapter.TryParseOutboundMessage(valid.Replace("\"1.0\"", "\"2.0\"", StringComparison.Ordinal), out _));
        Assert.False(OrientationMapBridgeAdapter.TryParseOutboundMessage("{\"contract\":\"orientation.host-bridge\",\"version\":\"1.0\",\"type\":\"bridge.ready\"}", out _));
    }

    [Fact]
    public void Feature_selection_is_accepted_only_for_the_vocation_map_source()
    {
        const string selected = "{\"contract\":\"orientation.host-bridge\",\"version\":\"1.0\",\"type\":\"feature.selected\",\"payload\":{\"featureRef\":\"feature-1\",\"sourceRef\":\"vocation.map_projection\"}}";
        Assert.True(OrientationMapBridgeAdapter.TryParseOutboundMessage(selected, out var message));
        Assert.NotNull(message);
        Assert.True(OrientationMapBridgeAdapter.TryGetSelectedFeatureRef(message, out var featureRef));
        Assert.Equal("feature-1", featureRef);

        const string foreign = "{\"contract\":\"orientation.host-bridge\",\"version\":\"1.0\",\"type\":\"feature.selected\",\"payload\":{\"featureRef\":\"feature-1\",\"sourceRef\":\"foreign.map\"}}";
        Assert.True(OrientationMapBridgeAdapter.TryParseOutboundMessage(foreign, out var foreignMessage));
        Assert.NotNull(foreignMessage);
        Assert.False(OrientationMapBridgeAdapter.TryGetSelectedFeatureRef(foreignMessage, out _));
    }
}
