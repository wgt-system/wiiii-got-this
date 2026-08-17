using WiiiiGotThis.Application;
using WiiiiGotThis.Infrastructure;

namespace WiiiiGotThis.Infrastructure.Tests;

public sealed class AtlasAppearancePreferenceStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"wgt-atlas-{Guid.NewGuid():N}");

    [Fact]
    public async Task Store_round_trips_theme_preference()
    {
        var path = Path.Combine(directory, "appearance.json");
        var store = new JsonAtlasAppearancePreferenceStore(path);

        Assert.Null(await store.LoadThemeAsync());

        await store.SaveThemeAsync(AtlasThemePreference.Machine);

        Assert.Equal(AtlasThemePreference.Machine, await store.LoadThemeAsync());
    }

    [Fact]
    public async Task Store_treats_malformed_or_unknown_theme_as_no_preference()
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "appearance.json");
        var store = new JsonAtlasAppearancePreferenceStore(path);

        await File.WriteAllTextAsync(path, "{not json");
        Assert.Null(await store.LoadThemeAsync());

        await File.WriteAllTextAsync(path, "{\"AtlasTheme\":\"future-theme\"}");
        Assert.Null(await store.LoadThemeAsync());
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }
}
