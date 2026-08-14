using System.Globalization;
using System.Text.Json;
using System.Text;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Integrations.Vocation;

public sealed class VocationMapProjectionContractReader
{
    public static VocationMapProjection Read(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            using var document = JsonDocument.Parse(json);
            return Read(document.RootElement);
        }
        catch (VocationPublishedContractValidationException)
        {
            throw;
        }
        catch (JsonException)
        {
            throw Malformed("The Vocation map publication is not valid JSON.");
        }
    }

    public static VocationMapProjection Read(ReadOnlyMemory<byte> utf8Json)
    {
        try
        {
            using var document = JsonDocument.Parse(utf8Json);
            return Read(document.RootElement);
        }
        catch (VocationPublishedContractValidationException)
        {
            throw;
        }
        catch (JsonException)
        {
            throw Malformed("The Vocation map publication is not valid JSON.");
        }
    }

    private static VocationMapProjection Read(JsonElement root)
    {
        var properties = ObjectProperties(root, "capability", "contract_version", "publication", "features");
        var capability = RequiredString(properties, "capability");
        if (!string.Equals(capability, VocationIntegrationMetadata.MapProjectionCapabilityValue, StringComparison.Ordinal))
            throw new VocationPublishedContractValidationException(VocationContractFailureKind.UnexpectedCapability, "The publication contains an unexpected capability.");

        var contractVersion = RequiredString(properties, "contract_version");
        if (!string.Equals(contractVersion, VocationIntegrationMetadata.MapProjectionContractVersionValue, StringComparison.Ordinal))
            throw new VocationPublishedContractValidationException(VocationContractFailureKind.UnsupportedContractVersion, "The Vocation map contract version is not supported.", contractVersion);

        var publication = ReadPublication(properties["publication"]);
        var features = ReadFeatures(properties["features"]);
        return new(publication.PublicationRef, publication.GeneratedAt, features);
    }

    private static (string PublicationRef, VocationContractTimestamp GeneratedAt) ReadPublication(JsonElement value)
    {
        var properties = ObjectProperties(value, "publication_ref", "generated_at");
        var publicationRef = OpaqueRef(properties, "publication_ref");
        var generatedAtText = RequiredString(properties, "generated_at");
        if (!TryParseRfc3339(generatedAtText, out var generatedAt))
            throw Malformed("The publication generated_at is not a valid date-time.");
        return (publicationRef, new VocationContractTimestamp(generatedAtText, generatedAt));
    }

    private static List<VocationMapFeature> ReadFeatures(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array) throw Malformed("The features property must be an array.");
        var result = new List<VocationMapFeature>();
        foreach (var feature in value.EnumerateArray()) result.Add(ReadFeature(feature));
        return result;
    }

    private static VocationMapFeature ReadFeature(JsonElement value)
    {
        var properties = ObjectProperties(value, "feature_ref", "opportunity_ref", "title", "company", "work_location", "coordinates");
        var title = RequiredString(properties, "title");
        if (title.Length == 0) throw Malformed("A map feature title must not be empty.");
        return new(
            OpaqueRef(properties, "feature_ref"),
            OpaqueRef(properties, "opportunity_ref"),
            title,
            ReadCompany(properties["company"]),
            ReadWorkLocation(properties["work_location"]),
            ReadCoordinates(properties["coordinates"]));
    }

    private static VocationMapCompany ReadCompany(JsonElement value)
    {
        var properties = ObjectProperties(value, "company_ref", "name");
        var name = RequiredString(properties, "name");
        if (name.Length == 0) throw Malformed("A company name must not be empty.");
        return new(OpaqueRef(properties, "company_ref"), name);
    }

    private static VocationMapWorkLocation ReadWorkLocation(JsonElement value)
    {
        var properties = ObjectProperties(value, "label", "precision");
        var label = RequiredString(properties, "label");
        if (label.Length == 0) throw Malformed("A work location label must not be empty.");
        var precision = RequiredString(properties, "precision");
        if (precision is not ("exact_address" or "site" or "city" or "region" or "approximate" or "unknown"))
            throw Malformed("A work location precision is not supported by the contract.");
        return new(label, precision);
    }

    private static VocationMapCoordinates ReadCoordinates(JsonElement value)
    {
        var properties = ObjectProperties(value, "latitude", "longitude");
        var latitude = Number(properties, "latitude");
        var longitude = Number(properties, "longitude");
        if (latitude is < -90 or > 90) throw Malformed("The map latitude is outside the accepted range.");
        if (longitude is < -180 or > 180) throw Malformed("The map longitude is outside the accepted range.");
        return new(latitude, longitude);
    }

    private static double Number(Dictionary<string, JsonElement> properties, string name)
    {
        var value = properties[name];
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var number) || !double.IsFinite(number))
            throw Malformed($"The contract property '{name}' must be a finite JSON number.");
        return number;
    }

    private static Dictionary<string, JsonElement> ObjectProperties(JsonElement value, params string[] expected)
    {
        if (value.ValueKind != JsonValueKind.Object) throw Malformed("A contract object was expected.");
        var expectedSet = expected.ToHashSet(StringComparer.Ordinal);
        var properties = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
        {
            if (!expectedSet.Contains(property.Name)) throw Malformed($"Unexpected contract property '{property.Name}'.");
            if (!properties.TryAdd(property.Name, property.Value)) throw Malformed($"Duplicate contract property '{property.Name}'.");
        }
        foreach (var required in expected)
            if (!properties.ContainsKey(required)) throw Malformed($"Required contract property '{required}' is missing.");
        return properties;
    }

    private static string OpaqueRef(Dictionary<string, JsonElement> properties, string name)
    {
        var value = RequiredString(properties, name);
        var codePointCount = value.EnumerateRunes().Count();
        if (codePointCount is < 1 or > 200) throw Malformed($"The opaque reference '{name}' has an invalid length.");
        return value;
    }

    private static string RequiredString(Dictionary<string, JsonElement> properties, string name)
    {
        var value = properties[name];
        if (value.ValueKind != JsonValueKind.String || value.GetString() is not { } text) throw Malformed($"The contract property '{name}' must be a string.");
        return text;
    }

    private static bool TryParseRfc3339(string value, out DateTimeOffset? normalizedUtc)
    {
        normalizedUtc = null;
        if (value.Length < 20 || value[10] is not ('T' or 't') || value[19] is not ('.' or 'Z' or 'z' or '+' or '-'))
            return false;

        var fractionStart = 19;
        var fractionLength = 0;
        if (value[fractionStart] == '.')
        {
            fractionStart++;
            while (fractionStart + fractionLength < value.Length && char.IsAsciiDigit(value[fractionStart + fractionLength]))
                fractionLength++;
            if (fractionLength == 0) return false;
        }

        var timezoneStart = fractionStart + fractionLength;
        var hasZuluTimezone = timezoneStart + 1 == value.Length && value[timezoneStart] is 'Z' or 'z';
        var hasNumericTimezone = timezoneStart + 6 == value.Length &&
            value[timezoneStart] is '+' or '-' &&
            value[timezoneStart + 3] == ':' &&
            Digits(value, timezoneStart + 1, 2) &&
            Digits(value, timezoneStart + 4, 2);
        if (!hasZuluTimezone && !hasNumericTimezone) return false;
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            return false;

        normalizedUtc = parsed.ToUniversalTime();
        return true;
    }

    private static bool Digits(string value, int start, int count)
    {
        if (start < 0 || start + count > value.Length) return false;
        for (var index = start; index < start + count; index++)
            if (!char.IsAsciiDigit(value[index])) return false;
        return true;
    }

    private static VocationPublishedContractValidationException Malformed(string message) =>
        new(VocationContractFailureKind.MalformedContract, message);
}
