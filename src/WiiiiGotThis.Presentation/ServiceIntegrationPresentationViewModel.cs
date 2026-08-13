using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Presentation;

public sealed class ServiceIntegrationPresentationViewModel(ServiceIntegrationListItem item)
{
    public ServiceIdentity ServiceIdentity => item.ServiceIdentity;
    public string DisplayName => item.DisplayName;
    public bool IsGloballyEnabled => item.IsGloballyEnabled;
    public bool? CurrentDeviceOverride => item.CurrentDeviceOverride;
    public bool IsEffectivelyEnabled => item.IsEffectivelyEnabled;
    public bool HasLastKnownPublication => item.HasLastKnownPublication;
    public bool HasRefreshBeenAttempted => item.HasRefreshBeenAttempted;
    public IntegrationRefreshStatus? LatestRefreshResult => item.LatestRefreshResult;
    public DateTimeOffset? LastRefreshAttemptedAtUtc => item.LastRefreshAttemptedAtUtc;
    public DateTimeOffset? LastSuccessfulRefreshAtUtc => item.LastSuccessfulRefreshAtUtc;

    public string GlobalEnablementText => IsGloballyEnabled ? "Enabled globally" : "Disabled globally";
    public string DeviceOverrideText => CurrentDeviceOverride switch
    {
        true => "Enabled on this device",
        false => "Disabled on this device",
        null => "Inherit global setting"
    };
    public string DeviceBehaviorText => CurrentDeviceOverride switch
    {
        true => "This device is explicitly enabled, regardless of the global setting.",
        false => "This device is explicitly disabled, regardless of the global setting.",
        null => "This device follows the global setting."
    };
    public string EffectiveEnablementText => IsEffectivelyEnabled ? "Enabled on this device" : "Disabled on this device";
    public string EnablementStatusText => EffectiveEnablementText;

    public string PublicationRefreshStatusText => !HasRefreshBeenAttempted
        ? "Known integration — publication not refreshed yet."
        : LatestRefreshResult switch
        {
            IntegrationRefreshStatus.Refreshed => "Publication refresh succeeded.",
            IntegrationRefreshStatus.AdapterFailed when HasLastKnownPublication => "Publication refresh failed — using the last-known publication.",
            IntegrationRefreshStatus.AdapterFailed => "Publication refresh failed — no valid publication is available.",
            IntegrationRefreshStatus.InvalidPublication when HasLastKnownPublication => "Provider returned an invalid publication — using the last-known publication.",
            IntegrationRefreshStatus.InvalidPublication => "Provider returned an invalid publication — no valid publication is available.",
            _ => "Known integration — publication not refreshed yet."
        };

    public string LastRefreshAttemptText => LastRefreshAttemptedAtUtc is { } attempted
        ? $"Last refresh attempt: {attempted.ToUniversalTime():u}"
        : "No refresh attempt recorded.";

    public string LastSuccessfulRefreshText => LastSuccessfulRefreshAtUtc is { } successful
        ? $"Last successful refresh: {successful.ToUniversalTime():u}"
        : "No successful publication recorded.";
}
