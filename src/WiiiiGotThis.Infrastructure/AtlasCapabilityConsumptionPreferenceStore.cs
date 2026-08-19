using System.Text.Json;
using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Infrastructure;

public sealed class JsonAtlasCapabilityConsumptionPreferenceStore(string filePath)
    : IAtlasCapabilityConsumptionPreferenceStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async ValueTask<IReadOnlyList<AtlasCapabilityConsumptionPreference>> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        var document = await LoadDocumentAsync(cancellationToken);
        if (document?.Entries is null || document.Entries.Count == 0)
            return Array.Empty<AtlasCapabilityConsumptionPreference>();

        var preferences = new List<AtlasCapabilityConsumptionPreference>(document.Entries.Count);
        foreach (var entry in document.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.ConsumerServiceId)
                || string.IsNullOrWhiteSpace(entry.ProviderServiceId)
                || string.IsNullOrWhiteSpace(entry.CapabilityId))
            {
                continue;
            }

            preferences.Add(new(
                new AtlasCapabilityConsumptionKey(
                    new ServiceIdentity(entry.ConsumerServiceId),
                    new ServiceIdentity(entry.ProviderServiceId),
                    new CapabilityIdentity(entry.CapabilityId)),
                entry.IsEnabled));
        }
        return preferences;
    }

    public async ValueTask SaveAsync(
        AtlasCapabilityConsumptionPreference preference,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preference);

        var existing = await LoadDocumentAsync(cancellationToken);
        var entries = existing?.Entries?.ToList() ?? [];
        var key = preference.Key;
        var matchingIndex = entries.FindIndex(entry =>
            string.Equals(entry.ConsumerServiceId, key.ConsumerServiceIdentity.Value, StringComparison.Ordinal)
            && string.Equals(entry.ProviderServiceId, key.ProviderServiceIdentity.Value, StringComparison.Ordinal)
            && string.Equals(entry.CapabilityId, key.CapabilityIdentity.Value, StringComparison.Ordinal));
        var stored = new PreferenceEntry(
            key.ConsumerServiceIdentity.Value,
            key.ProviderServiceIdentity.Value,
            key.CapabilityIdentity.Value,
            preference.IsEnabled);

        if (matchingIndex >= 0)
            entries[matchingIndex] = stored;
        else
            entries.Add(stored);

        entries.Sort(static (left, right) =>
        {
            var consumer = StringComparer.Ordinal.Compare(left.ConsumerServiceId, right.ConsumerServiceId);
            if (consumer != 0)
                return consumer;
            var provider = StringComparer.Ordinal.Compare(left.ProviderServiceId, right.ProviderServiceId);
            if (provider != 0)
                return provider;
            return StringComparer.Ordinal.Compare(left.CapabilityId, right.CapabilityId);
        });

        await SaveDocumentAsync(new PreferenceDocument(entries), cancellationToken);
    }

    private async ValueTask<PreferenceDocument?> LoadDocumentAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            await using var stream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<PreferenceDocument>(stream, JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private async ValueTask SaveDocumentAsync(PreferenceDocument document, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var temporaryPath = filePath + ".tmp";
        await using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);

        File.Move(temporaryPath, filePath, overwrite: true);
    }

    private sealed record PreferenceDocument(List<PreferenceEntry> Entries);

    private sealed record PreferenceEntry(
        string ConsumerServiceId,
        string ProviderServiceId,
        string CapabilityId,
        bool IsEnabled);
}
