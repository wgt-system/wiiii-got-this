using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Application;

/// <summary>
/// Identifies one product-to-provider capability-use relationship. Preference state is WGT
/// host configuration only; it does not transfer capability or domain ownership.
/// </summary>
public sealed record AtlasCapabilityConsumptionKey(
    ServiceIdentity ConsumerServiceIdentity,
    ServiceIdentity ProviderServiceIdentity,
    CapabilityIdentity CapabilityIdentity);

public sealed record AtlasCapabilityConsumptionPreference(
    AtlasCapabilityConsumptionKey Key,
    bool IsEnabled);

public interface IAtlasCapabilityConsumptionPreferenceStore
{
    ValueTask<IReadOnlyList<AtlasCapabilityConsumptionPreference>> LoadAsync(
        CancellationToken cancellationToken = default);

    ValueTask SaveAsync(
        AtlasCapabilityConsumptionPreference preference,
        CancellationToken cancellationToken = default);
}

public sealed class GetAtlasCapabilityConsumptionPreferencesUseCase(
    IAtlasCapabilityConsumptionPreferenceStore store)
{
    public ValueTask<IReadOnlyList<AtlasCapabilityConsumptionPreference>> GetAsync(
        CancellationToken cancellationToken = default) =>
        store.LoadAsync(cancellationToken);
}

public sealed class SetAtlasCapabilityConsumptionPreferenceUseCase(
    IAtlasCapabilityConsumptionPreferenceStore store)
{
    public ValueTask SetAsync(
        AtlasCapabilityConsumptionKey key,
        bool isEnabled,
        CancellationToken cancellationToken = default) =>
        store.SaveAsync(new AtlasCapabilityConsumptionPreference(key, isEnabled), cancellationToken);
}
