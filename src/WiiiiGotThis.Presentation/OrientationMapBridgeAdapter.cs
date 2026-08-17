using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace WiiiiGotThis.Presentation;

public sealed record OrientationHostBridgeMessage(string Type, JsonElement Payload);

public static class OrientationMapBridgeAdapter
{
    public const string Contract = "orientation.host-bridge";
    public const string Version = "1.0";
    public const string VocationMapSourceRef = "vocation.map_projection";

    public static string CreateSceneReplaceMessage(IEnumerable<VocationMapFeaturePresentationViewModel> features)
    {
        ArgumentNullException.ThrowIfNull(features);

        return CreateMessage("scene.replace", writer =>
        {
            writer.WritePropertyName("features");
            writer.WriteStartArray();
            foreach (var feature in features)
                WriteOrientationFeature(writer, feature);
            writer.WriteEndArray();

            writer.WritePropertyName("viewport");
            writer.WriteStartObject();
            writer.WriteString("kind", "automatic");
            writer.WriteNumber("padding", 48);
            writer.WriteNumber("maxZoom", 15);
            writer.WriteEndObject();
        });
    }

    public static string CreateCurrentPositionSetMessage(OrientationCurrentPosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        ValidatePosition(position);

        return CreateMessage("current-position.set", writer =>
        {
            writer.WritePropertyName("coordinate");
            writer.WriteStartObject();
            writer.WriteNumber("longitude", position.Longitude);
            writer.WriteNumber("latitude", position.Latitude);
            writer.WriteEndObject();
            writer.WriteNumber("accuracyMeters", position.AccuracyMeters);
            writer.WriteString(
                "observedAt",
                position.ObservedAt.UtcDateTime.ToString(
                    "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
                    CultureInfo.InvariantCulture));
        });
    }

    public static string CreateCurrentPositionClearMessage() =>
        CreateMessage("current-position.clear", static _ => { });

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

    private static string CreateMessage(string type, Action<Utf8JsonWriter> writePayload)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        writer.WriteStartObject();
        writer.WriteString("contract", Contract);
        writer.WriteString("version", Version);
        writer.WriteString("type", type);
        writer.WritePropertyName("payload");
        writer.WriteStartObject();
        writePayload(writer);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.Flush();
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void WriteOrientationFeature(
        Utf8JsonWriter writer,
        VocationMapFeaturePresentationViewModel feature)
    {
        writer.WriteStartObject();
        writer.WriteString("ref", feature.FeatureRef);
        writer.WriteString("sourceRef", VocationMapSourceRef);

        writer.WritePropertyName("coordinate");
        writer.WriteStartObject();
        writer.WriteNumber("longitude", feature.Longitude);
        writer.WriteNumber("latitude", feature.Latitude);
        writer.WriteEndObject();

        writer.WriteString("title", feature.Title);
        writer.WriteString("subtitle", $"{feature.CompanyName} · {feature.WorkLocationLabel}");

        writer.WritePropertyName("information");
        writer.WriteStartArray();
        writer.WriteStartObject();
        writer.WriteString("title", "Vocation");
        writer.WritePropertyName("rows");
        writer.WriteStartArray();
        WriteInformationRow(writer, "Company", feature.CompanyName);
        WriteInformationRow(writer, "Location", feature.WorkLocationLabel);
        WriteInformationRow(writer, "Precision", feature.WorkLocationPrecision);
        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    private static void WriteInformationRow(Utf8JsonWriter writer, string label, string value)
    {
        writer.WriteStartObject();
        writer.WriteString("label", label);
        writer.WriteString("value", value);
        writer.WriteEndObject();
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
