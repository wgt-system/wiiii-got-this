using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

public sealed partial class ShellViewModel
{
    private SetAtlasCapabilityConsumptionPreferenceUseCase? writeCapabilityConsumption;
    private IReadOnlyList<AtlasCapabilityConsumptionPreference> capabilityConsumptionPreferences =
        Array.Empty<AtlasCapabilityConsumptionPreference>();

    public void ConfigureCapabilityConsumptionPreferences(
        IReadOnlyList<AtlasCapabilityConsumptionPreference> preferences,
        SetAtlasCapabilityConsumptionPreferenceUseCase writer)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(writer);

        capabilityConsumptionPreferences = preferences.ToArray();
        writeCapabilityConsumption = writer;
        buildAtlasProjection.SetConsumptionPreferences(capabilityConsumptionPreferences);
    }

    public async Task<bool> ToggleCapabilityConsumptionAsync(
        AtlasConnectionPresentationViewModel connection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (!connection.IsCapabilityUse
            || !connection.IsUserConfigurable
            || writeCapabilityConsumption is null
            || connection.Source.ServiceIdentity is not { } consumerService
            || connection.Target.ServiceIdentity is not { } providerService
            || connection.Target.CapabilityIdentity is not { } capability)
        {
            return connection.IsEnabled;
        }

        var nextEnabled = !connection.IsEnabled;
        var key = new AtlasCapabilityConsumptionKey(
            consumerService,
            providerService,
            capability);

        try
        {
            await writeCapabilityConsumption.SetAsync(key, nextEnabled, cancellationToken);
        }
        catch (IOException)
        {
            StatusText = "Capability setting could not be saved.";
            return connection.IsEnabled;
        }
        catch (UnauthorizedAccessException)
        {
            StatusText = "Capability setting could not be saved.";
            return connection.IsEnabled;
        }

        var updated = capabilityConsumptionPreferences
            .Where(preference => preference.Key != key)
            .Append(new AtlasCapabilityConsumptionPreference(key, nextEnabled))
            .ToArray();
        capabilityConsumptionPreferences = updated;
        buildAtlasProjection.SetConsumptionPreferences(updated);
        await ReloadStateAsync();
        return nextEnabled;
    }
}
