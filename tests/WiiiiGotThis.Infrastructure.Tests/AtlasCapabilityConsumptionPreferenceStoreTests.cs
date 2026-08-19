using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;
using WiiiiGotThis.Infrastructure;

namespace WiiiiGotThis.Infrastructure.Tests;

public sealed class AtlasCapabilityConsumptionPreferenceStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"wgt-capability-use-{Guid.NewGuid():N}");

    [Fact]
    public async Task Missing_store_loads_as_empty_and_round_trips_one_product_relationship()
    {
        var path = Path.Combine(directory, "capability-consumption.json");
        var store = new JsonAtlasCapabilityConsumptionPreferenceStore(path);

        Assert.Empty(await store.LoadAsync());

        var preference = Preference("vocation", "conveyance", "conveyance.durable_delivery", false);
        await store.SaveAsync(preference);

        var loaded = Assert.Single(await store.LoadAsync());
        Assert.Equal(preference, loaded);
    }

    [Fact]
    public async Task Saving_the_same_relationship_replaces_its_state_without_overwriting_other_consumers()
    {
        var path = Path.Combine(directory, "capability-consumption.json");
        var store = new JsonAtlasCapabilityConsumptionPreferenceStore(path);

        await store.SaveAsync(Preference("vocation", "conveyance", "conveyance.durable_delivery", false));
        await store.SaveAsync(Preference("illumination", "conveyance", "conveyance.durable_delivery", true));
        await store.SaveAsync(Preference("vocation", "conveyance", "conveyance.durable_delivery", true));

        var loaded = await store.LoadAsync();
        Assert.Equal(2, loaded.Count);
        Assert.Contains(loaded, preference =>
            preference.Key.ConsumerServiceIdentity.Value == "vocation"
            && preference.IsEnabled);
        Assert.Contains(loaded, preference =>
            preference.Key.ConsumerServiceIdentity.Value == "illumination"
            && preference.IsEnabled);
    }

    [Fact]
    public async Task Malformed_store_is_treated_as_empty_without_inventing_preferences()
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "capability-consumption.json");
        await File.WriteAllTextAsync(path, "{not json");
        var store = new JsonAtlasCapabilityConsumptionPreferenceStore(path);

        Assert.Empty(await store.LoadAsync());
    }

    private static AtlasCapabilityConsumptionPreference Preference(
        string consumer,
        string provider,
        string capability,
        bool enabled) =>
        new(
            new AtlasCapabilityConsumptionKey(
                new ServiceIdentity(consumer),
                new ServiceIdentity(provider),
                new CapabilityIdentity(capability)),
            enabled);

    public void Dispose()
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }
}
