using WiiiiGotThis.Contracts;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Application;

public sealed class EnsureCurrentDeviceUseCase(ILocalDeviceStore devices)
{
    public async ValueTask<LocalDeviceConfiguration> GetOrCreateAsync(string suggestedDisplayName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suggestedDisplayName);
        var existing = await devices.LoadAsync(cancellationToken);
        if (existing is not null) return existing;

        var configuration = new LocalDeviceConfiguration(DeviceIdentity.New(), suggestedDisplayName.Trim());
        await devices.SaveAsync(configuration, cancellationToken);
        return configuration;
    }
}

public sealed class RegisterKnownIntegrationsUseCase(
    IIntegrationAdapterCatalog adapters,
    IServiceIntegrationStore integrations)
{
    public async ValueTask RegisterAsync(CancellationToken cancellationToken = default)
    {
        foreach (var adapter in adapters.Adapters)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await integrations.LoadAsync(adapter.ServiceId, cancellationToken) is null)
                await integrations.SaveAsync(new ServiceIntegration(adapter.ServiceId), cancellationToken);
        }
    }
}

public sealed record ServiceIntegrationListItem(
    ServiceIdentity ServiceIdentity,
    string DisplayName,
    bool IsGloballyEnabled,
    bool? CurrentDeviceOverride,
    bool IsEffectivelyEnabled,
    bool HasLastKnownPublication,
    bool HasRefreshBeenAttempted,
    IntegrationRefreshStatus? LatestRefreshResult,
    DateTimeOffset? LastRefreshAttemptedAtUtc,
    DateTimeOffset? LastSuccessfulRefreshAtUtc);

public sealed class ListServiceIntegrationsUseCase(
    IServiceIntegrationStore integrations,
    IIntegrationPublicationStore publications)
{
    public async ValueTask<IReadOnlyList<ServiceIntegrationListItem>> ListAsync(DeviceIdentity currentDevice, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currentDevice);
        var result = new List<ServiceIntegrationListItem>();
        foreach (var integration in await integrations.LoadAllAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var publicationState = await publications.LoadAsync(integration.ServiceIdentity, cancellationToken);
            var observation = publicationState.RefreshObservation;
            var overrideValue = integration.DeviceOverrides.TryGetValue(currentDevice, out var value)
                ? value == Enablement.Enabled
                : (bool?)null;
            result.Add(new(
                integration.ServiceIdentity,
                publicationState.Publication?.DisplayName ?? integration.ServiceIdentity.Value,
                integration.GlobalEnablement == Enablement.Enabled,
                overrideValue,
                integration.GetEffectiveEnablement(currentDevice) == Enablement.Enabled,
                publicationState.Publication is not null,
                observation.HasAttempted,
                observation.LatestResult,
                observation.LastAttemptedAtUtc,
                observation.LastSuccessfulRefreshAtUtc));
        }
        return result;
    }
}

public sealed class SetGlobalIntegrationEnablementUseCase(IServiceIntegrationStore integrations)
{
    public ValueTask EnableAsync(ServiceIdentity serviceIdentity, CancellationToken cancellationToken = default) => SetAsync(serviceIdentity, true, cancellationToken);
    public ValueTask DisableAsync(ServiceIdentity serviceIdentity, CancellationToken cancellationToken = default) => SetAsync(serviceIdentity, false, cancellationToken);

    private async ValueTask SetAsync(ServiceIdentity serviceIdentity, bool enabled, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serviceIdentity);
        var integration = await integrations.LoadAsync(serviceIdentity, cancellationToken)
            ?? throw new InvalidOperationException($"Unknown Service Integration '{serviceIdentity.Value}'.");
        if (enabled) integration.EnableGlobally(); else integration.DisableGlobally();
        await integrations.SaveAsync(integration, cancellationToken);
    }
}

public sealed class SetDeviceIntegrationOverrideUseCase(IServiceIntegrationStore integrations)
{
    public async ValueTask SetAsync(ServiceIdentity serviceIdentity, DeviceIdentity deviceIdentity, bool enabled, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceIdentity);
        ArgumentNullException.ThrowIfNull(deviceIdentity);
        var integration = await integrations.LoadAsync(serviceIdentity, cancellationToken)
            ?? throw new InvalidOperationException($"Unknown Service Integration '{serviceIdentity.Value}'.");
        integration.SetDeviceOverride(deviceIdentity, enabled ? Enablement.Enabled : Enablement.Disabled);
        await integrations.SaveAsync(integration, cancellationToken);
    }
}

public sealed class ClearDeviceIntegrationOverrideUseCase(IServiceIntegrationStore integrations)
{
    public async ValueTask ClearAsync(ServiceIdentity serviceIdentity, DeviceIdentity deviceIdentity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceIdentity);
        ArgumentNullException.ThrowIfNull(deviceIdentity);
        var integration = await integrations.LoadAsync(serviceIdentity, cancellationToken)
            ?? throw new InvalidOperationException($"Unknown Service Integration '{serviceIdentity.Value}'.");
        integration.ClearDeviceOverride(deviceIdentity);
        await integrations.SaveAsync(integration, cancellationToken);
    }
}
