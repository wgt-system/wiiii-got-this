using System.Globalization;
using System.Text.Json;

namespace WiiiiGotThis.Presentation;

public sealed record OrientationHostBridgeMessage(string Type, JsonElement Payload);

public static class OrientationMapBridgeAdapter
{
    public const string Contract = "orientation.host-bridge";
    public const string Version = "1.0";
    public const string VocationMapSourceRef = "vocation.map_projection";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string CreateSceneReplaceMessage(IEnumerable<VocationMapFeaturePresentationViewModel> features)
    {
        ArgumentNullException.ThrowIfNull(features);

        return JsonSerializer.Serialize(new
        {
            contract = Contract,
            version = Version,
            type = "scene.replace",
            payload = new
            {
                features = features.Select(ToOrientationFeature).ToArray(),
                viewport = new
                {
                    kind = "automatic",
                    padding = 48,
                    maxZoom = 15,
                },
            },
        }, JsonOptions);
    }

    public static string CreateCurrentPositionSetMessage(OrientationCurrentPosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        ValidatePosition(position);

        return JsonSerializer.Serialize(new
        {
            contract = Contract,
            version = Version,
            type = "current-position.set",
            payload = new
            {
                coordinate = new
                {
                    longitude = position.Longitude,
                    latitude = position.Latitude,
                },
                accuracyMeters = position.AccuracyMeters,
                observedAt = position.ObservedAt.UtcDateTime.ToString(
                    "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
                    CultureInfo.InvariantCulture),
            },
        }, JsonOptions);
    }

    public static string CreateCurrentPositionClearMessage() => JsonSerializer.Serialize(new
    {
        contract = Contract,
        version = Version,
        type = "current-position.clear",
        payload = new { },
    }, JsonOptions);

    public static bool TryParseOutboundMessage(string? body, out OrientationHostBridgeMessage? message)
    {
        message = null;
        if (string.IsNullOrWhiteSpace(body))
            return false;

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("contract", out var contract)
                || contract.GetString() != Contract
                || !root.TryGetProperty("version", out var version)
                || version.GetString() != Version
                || !root.TryGetProperty("type", out var type)
                || type.ValueKind != JsonValueKind.String
                || !root.TryGetProperty("payload", out var payload)
                || payload.ValueKind != JsonValueKind.Object)
                return false;

            message = new OrientationHostBridgeMessage(type.GetString()!, payload.Clone());
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static bool TryGetSelectedFeatureRef(OrientationHostBridgeMessage message, out string? featureRef)
    {
        ArgumentNullException.ThrowIfNull(message);
        featureRef = null;
        if (message.Type != "feature.selected"
            || !message.Payload.TryGetProperty("featureRef", out var featureRefElement)
            || featureRefElement.ValueKind != JsonValueKind.String
            || !message.Payload.TryGetProperty("sourceRef", out var sourceRefElement)
            || sourceRefElement.ValueKind != JsonValueKind.String
            || sourceRefElement.GetString() != VocationMapSourceRef)
            return false;

        featureRef = featureRefElement.GetString();
        return !string.IsNullOrWhiteSpace(featureRef);
    }

    private static object ToOrientationFeature(VocationMapFeaturePresentationViewModel feature)
    {
        return new
        {
            @ref = feature.FeatureRef,
            sourceRef = VocationMapSourceRef,
            coordinate = new
            {
                longitude = feature.Longitude,
                latitude = feature.Latitude,
            },
            title = feature.Title,
            subtitle = $"{feature.CompanyName} · {feature.WorkLocationLabel}",
            information = new[]
            {
                new
                {
                    title = "Vocation",
                    rows = new[]
                    {
                        new { label = "Company", value = feature.CompanyName },
                        new { label = "Location", value = feature.WorkLocationLabel },
                        new { label = "Precision", value = feature.WorkLocationPrecision },
                    },
                },
            },
        };
    }

    private static void ValidatePosition(OrientationCurrentPosition position)
    {
        if (!double.IsFinite(position.Longitude) || position.Longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(position), "Longitude must be finite and between -180 and 180 degrees.");
        if (!double.IsFinite(position.Latitude) || position.Latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(position), "Latitude must be finite and between -90 and 90 degrees.");
        if (!double.IsFinite(position.AccuracyMeters) || position.AccuracyMeters < 0)
            throw new ArgumentOutOfRangeException(nameof(position), "Accuracy must be finite and non-negative.");
    }
}
