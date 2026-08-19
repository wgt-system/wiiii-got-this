using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

public sealed partial class ShellViewModel
{
    private SetAtlasCapabilityConsumptionPreferenceUseCase? writeCapabilityConsumption;
    private AtlasCapabilityConsumptionPreference[] capabilityConsumptionPreferences = [];

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

        var current = AtlasConnections.FirstOrDefault(candidate =>
                string.Equals(candidate.Model.ConnectionId, connection.Model.ConnectionId, StringComparison.Ordinal))
            ?? connection;

        if (!current.IsCapabilityUse
            || !current.IsUserConfigurable
            || writeCapabilityConsumption is null
            || current.Source.ServiceIdentity is not { } consumerService
            || current.Target.ServiceIdentity is not { } providerService
            || current.Target.CapabilityIdentity is not { } capability)
        {
            return current.IsEnabled;
        }

        var nextEnabled = !current.IsEnabled;
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
            return current.IsEnabled;
        }
        catch (UnauthorizedAccessException)
        {
            StatusText = "Capability setting could not be saved.";
            return current.IsEnabled;
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
