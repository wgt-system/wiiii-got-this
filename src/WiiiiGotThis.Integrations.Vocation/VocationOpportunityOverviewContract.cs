using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text;
using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Integrations.Vocation;

public static class VocationIntegrationMetadata
{
    public const string OpportunityOverviewCapabilityValue = "vocation.opportunity_overview";
    public const string OpportunityOverviewContractVersionValue = "1.0";
    public static readonly ServiceIdentity ServiceId = new("vocation");
    public const string ServiceDisplayName = "Vocation";
    public static readonly CapabilityIdentity OpportunityOverviewCapability = new(OpportunityOverviewCapabilityValue);
    public const string OpportunityOverviewTitle = "Opportunity Overview";
    public const string MapProjectionCapabilityValue = "vocation.map_projection";
    public const string MapProjectionContractVersionValue = "1.0";
    public static readonly CapabilityIdentity MapProjectionCapability = new(MapProjectionCapabilityValue);
    public const string MapProjectionTitle = "Map Projection";
    public static readonly Version MapProjectionContractVersion = new(1, 0);
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
        catch (JsonException)
        {
            throw Malformed("The Vocation publication is not valid JSON.");
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
        catch (JsonException)
        {
            throw Malformed("The Vocation publication is not valid JSON.");
        }
    }

    private static VocationOpportunityOverview Read(JsonElement root)
    {
        var properties = ObjectProperties(root, "capability", "contract_version", "publication", "opportunities");
        var capability = RequiredString(properties, "capability");
        if (!string.Equals(capability, VocationIntegrationMetadata.OpportunityOverviewCapabilityValue, StringComparison.Ordinal))
            throw new VocationPublishedContractValidationException(VocationContractFailureKind.UnexpectedCapability, "The publication contains an unexpected capability.");

        var contractVersion = RequiredString(properties, "contract_version");
        if (!string.Equals(contractVersion, VocationIntegrationMetadata.OpportunityOverviewContractVersionValue, StringComparison.Ordinal))
            throw new VocationPublishedContractValidationException(VocationContractFailureKind.UnsupportedContractVersion, "The Vocation contract version is not supported.", contractVersion);

        var publication = ReadPublication(properties["publication"]);
        var opportunities = ReadOpportunities(properties["opportunities"]);
        return new(publication.PublicationRef, publication.GeneratedAt, opportunities);
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
        if (postingCountValue.ValueKind != JsonValueKind.Number || !TryParseNonNegativeInteger(postingCountValue.GetRawText(), out var postingCount))
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

    private static string? NullableString(Dictionary<string, JsonElement> properties, string name)
    {
        var value = properties[name];
        if (value.ValueKind == JsonValueKind.Null) return null;
        if (value.ValueKind != JsonValueKind.String || value.GetString() is not { } text) throw Malformed($"The contract property '{name}' must be a string or null.");
        return text;
    }

    private static bool TryParseNonNegativeInteger(string raw, out BigInteger value)
    {
        value = BigInteger.Zero;
        var cursor = 0;
        var negative = raw.Length > 0 && raw[0] == '-';
        if (negative) cursor++;
        var exponentMarker = raw.IndexOfAny(['e', 'E'], cursor);
        var mantissaEnd = exponentMarker < 0 ? raw.Length : exponentMarker;
        var decimalPoint = raw.IndexOf('.', cursor, mantissaEnd - cursor);
        var fractionDigits = decimalPoint < 0 ? 0 : mantissaEnd - decimalPoint - 1;
        var digits = decimalPoint < 0
            ? raw[cursor..mantissaEnd]
            : string.Concat(raw[cursor..decimalPoint], raw[(decimalPoint + 1)..mantissaEnd]);
        if (digits.Length == 0 || !BigInteger.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var coefficient)) return false;
        if (coefficient.IsZero) return true;

        var exponent = BigInteger.Zero;
        if (exponentMarker >= 0 && !BigInteger.TryParse(raw[(exponentMarker + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out exponent)) return false;
        var scale = exponent - fractionDigits;
        if (scale >= 0)
        {
            if (scale > int.MaxValue) return false;
            value = coefficient * BigInteger.Pow(10, (int)scale);
        }
        else
        {
            var divisorScale = -scale;
            if (divisorScale > int.MaxValue) return false;
            var divisor = BigInteger.Pow(10, (int)divisorScale);
            if (coefficient % divisor != 0) return false;
            value = coefficient / divisor;
        }
        return !negative;
    }

    private static bool TryParseRfc3339(string value, out DateTimeOffset? normalizedUtc)
    {
        normalizedUtc = null;
        if (value.Length < 20 || !Digits(value, 0, 4) || value[4] != '-' || !Digits(value, 5, 2) || value[7] != '-' || !Digits(value, 8, 2) || (value[10] is not ('T' or 't')) || !Digits(value, 11, 2) || value[13] != ':' || !Digits(value, 14, 2) || value[16] != ':' || !Digits(value, 17, 2)) return false;
        var fractionStart = 19;
        var fractionLength = 0;
        if (fractionStart < value.Length && value[fractionStart] == '.')
        {
            fractionStart++;
            while (fractionStart + fractionLength < value.Length && IsAsciiDigit(value[fractionStart + fractionLength])) fractionLength++;
            if (fractionLength == 0) return false;
        }
        var timezoneStart = fractionStart + fractionLength;
        TimeSpan offset;
        if (timezoneStart + 1 == value.Length && value[timezoneStart] is 'Z' or 'z') offset = TimeSpan.Zero;
        else
        {
            if (timezoneStart + 6 != value.Length || value[timezoneStart] is not ('+' or '-') || value[timezoneStart + 3] != ':' || !Digits(value, timezoneStart + 1, 2) || !Digits(value, timezoneStart + 4, 2)) return false;
            var offsetHours = Number(value, timezoneStart + 1, 2); var zoneMinutes = Number(value, timezoneStart + 4, 2);
            if (offsetHours > 23 || zoneMinutes > 59) return false;
            offset = new TimeSpan(offsetHours, zoneMinutes, 0);
            if (value[timezoneStart] == '-') offset = -offset;
        }

        var year = Number(value, 0, 4); var month = Number(value, 5, 2); var day = Number(value, 8, 2);
        var hour = Number(value, 11, 2); var minute = Number(value, 14, 2); var second = Number(value, 17, 2);
        if (month is < 1 or > 12 || day < 1 || day > DaysInMonth(year, month) || hour > 23 || minute > 59 || second > 60) return false;
        var offsetMinutes = (int)offset.TotalMinutes;
        AddMinutesToUtc(year, month, day, hour, minute, -offsetMinutes, out var utcYear, out var utcMonth, out var utcDay, out var utcHour, out var utcMinute);
        if (second == 60 && (!IsKnownLeapSecondDate(utcYear, utcMonth, utcDay) || utcHour != 23 || utcMinute != 59)) return false;
        var ticks = 0L;
        for (var index = 0; index < Math.Min(fractionLength, 7); index++) ticks = ticks * 10 + (value[fractionStart + index] - '0');
        for (var index = fractionLength; index < 7; index++) ticks *= 10;
        if (utcYear is < 1 or > 9999) return true;
        try
        {
            var baseSecond = second == 60 ? 59 : second;
            var normalized = new DateTimeOffset(utcYear, utcMonth, utcDay, utcHour, utcMinute, baseSecond, TimeSpan.Zero).AddTicks(ticks);
            if (second == 60) normalized = normalized.AddSeconds(1);
            normalizedUtc = normalized;
            return true;
        }
        catch (ArgumentOutOfRangeException) { return false; }
    }

    private static int DaysInMonth(int year, int month) => month switch
    {
        2 => IsLeapYear(year) ? 29 : 28,
        4 or 6 or 9 or 11 => 30,
        _ => 31
    };

    private static bool IsLeapYear(int year) => year % 400 == 0 || (year % 4 == 0 && year % 100 != 0);

    private static void AddMinutesToUtc(int year, int month, int day, int hour, int minute, int deltaMinutes, out int utcYear, out int utcMonth, out int utcDay, out int utcHour, out int utcMinute)
    {
        var totalMinutes = hour * 60 + minute + deltaMinutes;
        while (totalMinutes < 0) { totalMinutes += 1440; PreviousDay(ref year, ref month, ref day); }
        while (totalMinutes >= 1440) { totalMinutes -= 1440; NextDay(ref year, ref month, ref day); }
        utcYear = year; utcMonth = month; utcDay = day; utcHour = totalMinutes / 60; utcMinute = totalMinutes % 60;
    }

    private static void PreviousDay(ref int year, ref int month, ref int day)
    {
        if (--day >= 1) return;
        if (--month < 1) { month = 12; year--; }
        day = DaysInMonth(year, month);
    }

    private static void NextDay(ref int year, ref int month, ref int day)
    {
        if (++day <= DaysInMonth(year, month)) return;
        day = 1;
        if (++month > 12) { month = 1; year++; }
    }

    private static bool IsKnownLeapSecondDate(int year, int month, int day) =>
        LeapSecondDates.Contains((year, month, day));

    private static readonly HashSet<(int Year, int Month, int Day)> LeapSecondDates =
    [
        (1972, 6, 30), (1972, 12, 31), (1973, 12, 31), (1974, 12, 31), (1975, 12, 31), (1976, 12, 31),
        (1977, 12, 31), (1978, 12, 31), (1979, 12, 31), (1981, 6, 30), (1982, 6, 30), (1983, 6, 30),
        (1985, 6, 30), (1987, 12, 31), (1989, 12, 31), (1990, 12, 31), (1992, 6, 30), (1993, 6, 30),
        (1994, 6, 30), (1995, 12, 31), (1997, 6, 30), (1998, 12, 31), (2005, 12, 31), (2008, 12, 31),
        (2012, 6, 30), (2015, 6, 30), (2016, 12, 31)
    ];

    private static bool Digits(string value, int start, int count)
    {
        if (start < 0 || start + count > value.Length) return false;
        for (var index = start; index < start + count; index++) if (!IsAsciiDigit(value[index])) return false;
        return true;
    }

    private static bool IsAsciiDigit(char value) => value is >= '0' and <= '9';
    private static int Number(string value, int start, int count) => int.Parse(value.AsSpan(start, count), CultureInfo.InvariantCulture);

    private static VocationPublishedContractValidationException Malformed(string message) =>
        new(VocationContractFailureKind.MalformedContract, message);
}
