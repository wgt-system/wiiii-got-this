namespace WiiiiGotThis.Application;

public enum AtlasThemePreference
{
    Technical,
    Elegant,
    Machine,
    World
}

public interface IAtlasAppearancePreferenceStore
{
    ValueTask<AtlasThemePreference?> LoadThemeAsync(CancellationToken cancellationToken = default);
    ValueTask SaveThemeAsync(AtlasThemePreference theme, CancellationToken cancellationToken = default);
}

public sealed class GetAtlasAppearancePreferenceUseCase(IAtlasAppearancePreferenceStore store)
{
    public ValueTask<AtlasThemePreference?> GetThemeAsync(CancellationToken cancellationToken = default) =>
        store.LoadThemeAsync(cancellationToken);
}

public sealed class SetAtlasAppearancePreferenceUseCase(IAtlasAppearancePreferenceStore store)
{
    public ValueTask SetThemeAsync(AtlasThemePreference theme, CancellationToken cancellationToken = default) =>
        store.SaveThemeAsync(theme, cancellationToken);
}
