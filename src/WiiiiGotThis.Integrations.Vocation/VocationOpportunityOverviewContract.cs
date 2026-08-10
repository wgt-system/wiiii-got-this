using System.Globalization;
using System.Text.Json;
using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Integrations.Vocation;

public static class VocationIntegrationMetadata
{
    public static readonly ServiceIdentity ServiceId = new("vocation");
    public const string ServiceDisplayName = "Vocation";
    public static readonly CapabilityIdentity OpportunityOverviewCapability = new("vocation.opportunity_overview");
    public const string OpportunityOverviewTitle = "Opportunity Overview";
    public static readonly Version OpportunityOverviewContractVersion = new(1, 0);
}

public enum VocationContractFailureKind
{
    MalformedContract,
    UnexpectedCapability,
    UnsupportedContractVersion
}

public sealed class VocationPublishedContractValidationException : Exception
{
    public VocationPublishedContractValidationException(VocationContractFailureKind kind, string message, string? unsupportedVersion = null)
        : base(message)
    {
        Kind = kind;
        UnsupportedVersion = unsupportedVersion;
    }

    public VocationContractFailureKind Kind { get; }
    public string? UnsupportedVersion { get; }
}

public sealed class VocationOpportunityOverviewContractReader
{
    public static VocationOpportunityOverview Read(string json)
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
        catch (JsonException exception)
        {
            throw Malformed("The Vocation publication is not valid JSON.", exception);
        }
    }

    public static VocationOpportunityOverview Read(ReadOnlyMemory<byte> utf8Json)
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
        catch (JsonException exception)
        {
            throw Malformed("The Vocation publication is not valid JSON.", exception);
        }
    }

    private static VocationOpportunityOverview Read(JsonElement root)
    {
        var properties = ObjectProperties(root, "capability", "contract_version", "publication", "opportunities");
        var capability = RequiredString(properties, "capability");
        if (!string.Equals(capability, "vocation.opportunity_overview", StringComparison.Ordinal))
            throw new VocationPublishedContractValidationException(VocationContractFailureKind.UnexpectedCapability, "The publication contains an unexpected capability.");

        var contractVersion = RequiredString(properties, "contract_version");
        if (!string.Equals(contractVersion, "1.0", StringComparison.Ordinal))
            throw new VocationPublishedContractValidationException(VocationContractFailureKind.UnsupportedContractVersion, "The Vocation contract version is not supported.", contractVersion);

        var publication = ReadPublication(properties["publication"]);
        var opportunities = ReadOpportunities(properties["opportunities"]);
        return new(publication.PublicationRef, publication.GeneratedAt, opportunities);
    }

    private static (string PublicationRef, DateTimeOffset GeneratedAt) ReadPublication(JsonElement value)
    {
        var properties = ObjectProperties(value, "publication_ref", "generated_at");
        var publicationRef = OpaqueRef(properties, "publication_ref");
        var generatedAtText = RequiredString(properties, "generated_at");
        if (!IsJsonSchemaDateTime(generatedAtText) || !DateTimeOffset.TryParse(generatedAtText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var generatedAt))
            throw Malformed("The publication generated_at is not a valid date-time.");
        return (publicationRef, generatedAt);
    }

    private static List<VocationOpportunity> ReadOpportunities(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array) throw Malformed("The opportunities property must be an array.");
        var result = new List<VocationOpportunity>();
        foreach (var opportunity in value.EnumerateArray()) result.Add(ReadOpportunity(opportunity));
        return result;
    }

    private static VocationOpportunity ReadOpportunity(JsonElement value)
    {
        var properties = ObjectProperties(value, "opportunity_ref", "title", "company", "work_locations", "posting_count");
        var title = RequiredString(properties, "title");
        if (title.Length == 0) throw Malformed("An opportunity title must not be empty.");
        var postingCountValue = properties["posting_count"];
        if (postingCountValue.ValueKind != JsonValueKind.Number || !postingCountValue.TryGetInt64(out var postingCount) || postingCount < 0)
            throw Malformed("An opportunity posting_count must be a non-negative JSON integer.");
        return new(
            OpaqueRef(properties, "opportunity_ref"),
            title,
            ReadCompany(properties["company"]),
            ReadWorkLocations(properties["work_locations"]),
            postingCount);
    }

    private static VocationCompany ReadCompany(JsonElement value)
    {
        var properties = ObjectProperties(value, "company_ref", "name");
        var name = RequiredString(properties, "name");
        if (name.Length == 0) throw Malformed("A company name must not be empty.");
        return new(OpaqueRef(properties, "company_ref"), name);
    }

    private static List<VocationWorkLocation> ReadWorkLocations(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array) throw Malformed("The work_locations property must be an array.");
        var result = new List<VocationWorkLocation>();
        foreach (var location in value.EnumerateArray()) result.Add(ReadWorkLocation(location));
        return result;
    }

    private static VocationWorkLocation ReadWorkLocation(JsonElement value)
    {
        var properties = ObjectProperties(value, "label", "city", "region", "country_code", "precision");
        var label = RequiredString(properties, "label");
        if (label.Length == 0) throw Malformed("A work location label must not be empty.");
        var city = NullableString(properties, "city");
        var region = NullableString(properties, "region");
        var countryCode = NullableString(properties, "country_code");
        if (countryCode is not null && (countryCode.Length != 2 || countryCode.Any(character => character is < 'A' or > 'Z')))
            throw Malformed("A work location country_code must contain two uppercase ASCII letters.");
        var precision = RequiredString(properties, "precision");
        if (precision is not ("exact_address" or "site" or "city" or "region" or "approximate" or "unknown"))
            throw Malformed("A work location precision is not supported by the contract.");
        return new(label, city, region, countryCode, precision);
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
        if (value.Length is < 1 or > 200) throw Malformed($"The opaque reference '{name}' has an invalid length.");
        return value;
    }

    private static string RequiredString(Dictionary<string, JsonElement> properties, string name)
    {
        var value = properties[name];
        if (value.ValueKind != JsonValueKind.String || value.GetString() is not { } text) throw Malformed($"The contract property '{name}' must be a string.");
        return text;
    }

    private static string? NullableString(Dictionary<string, JsonElement> properties, string name)
    {
        var value = properties[name];
        if (value.ValueKind == JsonValueKind.Null) return null;
        if (value.ValueKind != JsonValueKind.String || value.GetString() is not { } text) throw Malformed($"The contract property '{name}' must be a string or null.");
        return text;
    }

    private static bool IsJsonSchemaDateTime(string value)
    {
        if (value.Length < 20 || value[10] != 'T') return false;
        var timezoneStart = value.LastIndexOfAny(['Z', '+', '-']);
        if (timezoneStart < 19) return false;
        return value[^1] == 'Z' || (value.Length - timezoneStart == 6 && value[^3] == ':');
    }

    private static VocationPublishedContractValidationException Malformed(string message, Exception? inner = null) =>
        new(VocationContractFailureKind.MalformedContract, message);
}
