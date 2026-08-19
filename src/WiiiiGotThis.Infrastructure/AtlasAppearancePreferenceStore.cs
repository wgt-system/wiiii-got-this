using System.Text.Json;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Infrastructure;

public sealed class JsonAtlasAppearancePreferenceStore(string filePath) : IAtlasAppearancePreferenceStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async ValueTask<AtlasThemePreference?> LoadThemeAsync(CancellationToken cancellationToken = default)
    {
        var document = await LoadDocumentAsync(cancellationToken);
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

    public async ValueTask SaveThemeAsync(AtlasThemePreference theme, CancellationToken cancellationToken = default)
    {
        var existing = await LoadDocumentAsync(cancellationToken);
        await SaveDocumentAsync(
            new AppearanceDocument(ToStorage(theme), existing?.AtlasMotion),
            cancellationToken);
    }

    public async ValueTask<AtlasMotionPreference?> LoadMotionAsync(CancellationToken cancellationToken = default)
    {
        var document = await LoadDocumentAsync(cancellationToken);
        return document?.AtlasMotion?.Trim().ToLowerInvariant() switch
        {
            "full" => AtlasMotionPreference.Full,
            "reduced" => AtlasMotionPreference.Reduced,
            null or "" => null,
            _ => null
        };
    }

    public async ValueTask SaveMotionAsync(AtlasMotionPreference motion, CancellationToken cancellationToken = default)
    {
        var existing = await LoadDocumentAsync(cancellationToken);
        await SaveDocumentAsync(
            new AppearanceDocument(existing?.AtlasTheme, ToStorage(motion)),
            cancellationToken);
    }

    private async ValueTask<AppearanceDocument?> LoadDocumentAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            await using var stream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<AppearanceDocument>(stream, JsonOptions, cancellationToken);
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

    private async ValueTask SaveDocumentAsync(AppearanceDocument document, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

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

    private static string ToStorage(AtlasMotionPreference motion) => motion switch
    {
        AtlasMotionPreference.Full => "full",
        AtlasMotionPreference.Reduced => "reduced",
        _ => throw new ArgumentOutOfRangeException(nameof(motion))
    };

    private sealed record AppearanceDocument(string? AtlasTheme, string? AtlasMotion);
}
