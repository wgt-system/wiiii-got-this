namespace WiiiiGotThis.Application;

public enum AtlasThemePreference
{
    Technical,
    Elegant,
    Machine,
    World
}

public enum AtlasMotionPreference
{
    Full,
    Reduced
}

public interface IAtlasAppearancePreferenceStore
{
    ValueTask<AtlasThemePreference?> LoadThemeAsync(CancellationToken cancellationToken = default);
    ValueTask SaveThemeAsync(AtlasThemePreference theme, CancellationToken cancellationToken = default);
    ValueTask<AtlasMotionPreference?> LoadMotionAsync(CancellationToken cancellationToken = default);
    ValueTask SaveMotionAsync(AtlasMotionPreference motion, CancellationToken cancellationToken = default);
}

public sealed class GetAtlasAppearancePreferenceUseCase(IAtlasAppearancePreferenceStore store)
{
    public ValueTask<AtlasThemePreference?> GetThemeAsync(CancellationToken cancellationToken = default) =>
        store.LoadThemeAsync(cancellationToken);

    public ValueTask<AtlasMotionPreference?> GetMotionAsync(CancellationToken cancellationToken = default) =>
        store.LoadMotionAsync(cancellationToken);
}

public sealed class SetAtlasAppearancePreferenceUseCase(IAtlasAppearancePreferenceStore store)
{
    public ValueTask SetThemeAsync(AtlasThemePreference theme, CancellationToken cancellationToken = default) =>
        store.SaveThemeAsync(theme, cancellationToken);

    public ValueTask SetMotionAsync(AtlasMotionPreference motion, CancellationToken cancellationToken = default) =>
        store.SaveMotionAsync(motion, cancellationToken);
}
