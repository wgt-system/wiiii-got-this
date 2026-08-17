using System.Text.Json;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Infrastructure;

public sealed class JsonAtlasAppearancePreferenceStore(string filePath) : IAtlasAppearancePreferenceStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async ValueTask<AtlasThemePreference?> LoadThemeAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            await using var stream = File.OpenRead(filePath);
            var document = await JsonSerializer.DeserializeAsync<AppearanceDocument>(stream, JsonOptions, cancellationToken);
            return document?.AtlasTheme?.Trim().ToLowerInvariant() switch
            {
                "technical" => AtlasThemePreference.Technical,
                "elegant" => AtlasThemePreference.Elegant,
                "machine" => AtlasThemePreference.Machine,
                "world" => AtlasThemePreference.World,
                null or "" => null,
                _ => null
            };
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    public async ValueTask SaveThemeAsync(AtlasThemePreference theme, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var document = new AppearanceDocument(ToStorage(theme));
        var temporaryPath = filePath + ".tmp";
        await using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);

        File.Move(temporaryPath, filePath, overwrite: true);
    }

    private static string ToStorage(AtlasThemePreference theme) => theme switch
    {
        AtlasThemePreference.Technical => "technical",
        AtlasThemePreference.Elegant => "elegant",
        AtlasThemePreference.Machine => "machine",
        AtlasThemePreference.World => "world",
        _ => throw new ArgumentOutOfRangeException(nameof(theme))
    };

    private sealed record AppearanceDocument(string AtlasTheme);
}
