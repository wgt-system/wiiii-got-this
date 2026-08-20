using WiiiiGotThis.Application;
using WiiiiGotThis.Infrastructure;

namespace WiiiiGotThis.Infrastructure.Tests;

public sealed class AtlasAppearancePreferenceStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"wgt-atlas-{Guid.NewGuid():N}");

    [Fact]
    public async Task Store_round_trips_theme_and_motion_without_overwriting_each_other()
    {
        var path = Path.Combine(directory, "appearance.json");
        var store = new JsonAtlasAppearancePreferenceStore(path);

        Assert.Null(await store.LoadThemeAsync());
        Assert.Null(await store.LoadMotionAsync());

        await store.SaveThemeAsync(AtlasThemePreference.Machine);
        await store.SaveMotionAsync(AtlasMotionPreference.Reduced);

        Assert.Equal(AtlasThemePreference.Machine, await store.LoadThemeAsync());
        Assert.Equal(AtlasMotionPreference.Reduced, await store.LoadMotionAsync());

        await store.SaveThemeAsync(AtlasThemePreference.World);
        Assert.Equal(AtlasThemePreference.World, await store.LoadThemeAsync());
        Assert.Equal(AtlasMotionPreference.Reduced, await store.LoadMotionAsync());

        await store.SaveMotionAsync(AtlasMotionPreference.Full);
        Assert.Equal(AtlasThemePreference.World, await store.LoadThemeAsync());
        Assert.Equal(AtlasMotionPreference.Full, await store.LoadMotionAsync());
    }

    [Fact]
    public async Task Store_treats_malformed_or_unknown_appearance_values_as_no_preference()
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "appearance.json");
        var store = new JsonAtlasAppearancePreferenceStore(path);

        await File.WriteAllTextAsync(path, "{not json");
        Assert.Null(await store.LoadThemeAsync());
        Assert.Null(await store.LoadMotionAsync());

        await File.WriteAllTextAsync(path, "{\"AtlasTheme\":\"future-theme\",\"AtlasMotion\":\"hyper\"}");
        Assert.Null(await store.LoadThemeAsync());
        Assert.Null(await store.LoadMotionAsync());
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }
}
