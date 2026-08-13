using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Presentation;

public enum ShellSurface
{
    Home,
    Settings
}

public sealed partial class ShellViewModel : ObservableObject
{
    private readonly EnsureCurrentDeviceUseCase ensureCurrentDevice;
    private readonly RegisterKnownIntegrationsUseCase registerKnownIntegrations;
    private readonly RefreshPublicationsUseCase refreshPublications;
    private readonly ListServiceIntegrationsUseCase listServiceIntegrations;
    private readonly SetGlobalIntegrationEnablementUseCase setGlobalEnablement;
    private readonly SetDeviceIntegrationOverrideUseCase setDeviceOverride;
    private readonly ClearDeviceIntegrationOverrideUseCase clearDeviceOverride;
    private readonly ResolveCapabilityCatalogUseCase resolveCapabilityCatalog;
    private readonly GetVocationOpportunityOverviewUseCase? readVocationOpportunityOverview;
    private readonly string suggestedDeviceName;
    private readonly object initializationGate = new();
    private Task? initializationTask;

    [ObservableProperty] private string currentDeviceName = "Not initialized";
    [ObservableProperty] private DeviceIdentity? currentDeviceIdentity;
    [ObservableProperty] private ServiceIntegrationPresentationViewModel? selectedIntegration;
    [ObservableProperty] private CapabilityPresentationViewModel? selectedCapability;
    [ObservableProperty] private CapabilityPresentationViewModel? openedReferenceCapability;
    [ObservableProperty] private VocationOpportunityOverviewViewModel? openedVocationOpportunityOverview;
    [ObservableProperty] private string statusText = "Starting…";
    [ObservableProperty] private ShellSurface currentSurface = ShellSurface.Home;

    public ShellViewModel(
        EnsureCurrentDeviceUseCase ensureCurrentDevice,
        RegisterKnownIntegrationsUseCase registerKnownIntegrations,
        RefreshPublicationsUseCase refreshPublications,
        ListServiceIntegrationsUseCase listServiceIntegrations,
        SetGlobalIntegrationEnablementUseCase setGlobalEnablement,
        SetDeviceIntegrationOverrideUseCase setDeviceOverride,
        ClearDeviceIntegrationOverrideUseCase clearDeviceOverride,
        ResolveCapabilityCatalogUseCase resolveCapabilityCatalog,
        string suggestedDeviceName,
        GetVocationOpportunityOverviewUseCase? readVocationOpportunityOverview = null)
    {
        this.ensureCurrentDevice = ensureCurrentDevice;
        this.registerKnownIntegrations = registerKnownIntegrations;
        this.refreshPublications = refreshPublications;
        this.listServiceIntegrations = listServiceIntegrations;
        this.setGlobalEnablement = setGlobalEnablement;
        this.setDeviceOverride = setDeviceOverride;
        this.clearDeviceOverride = clearDeviceOverride;
        this.resolveCapabilityCatalog = resolveCapabilityCatalog;
        this.readVocationOpportunityOverview = readVocationOpportunityOverview;
        this.suggestedDeviceName = suggestedDeviceName;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        EnableGloballyCommand = new AsyncRelayCommand(EnableGloballyAsync, CanManageSelectedIntegration);
        DisableGloballyCommand = new AsyncRelayCommand(DisableGloballyAsync, CanManageSelectedIntegration);
        EnableOnThisDeviceCommand = new AsyncRelayCommand(EnableOnThisDeviceAsync, CanManageSelectedIntegration);
        DisableOnThisDeviceCommand = new AsyncRelayCommand(DisableOnThisDeviceAsync, CanManageSelectedIntegration);
        InheritGlobalSettingCommand = new AsyncRelayCommand(InheritGlobalSettingAsync, CanManageSelectedIntegration);
        OpenCapabilityCommand = new AsyncRelayCommand(OpenCapabilityAsync, CanOpenSelectedCapability);
        BackToCatalogCommand = new RelayCommand(() => { OpenedReferenceCapability = null; OpenedVocationOpportunityOverview = null; });
        ShowHomeCommand = new RelayCommand(() => CurrentSurface = ShellSurface.Home);
        ShowSettingsCommand = new RelayCommand(() => CurrentSurface = ShellSurface.Settings);
    }

    public ObservableCollection<ServiceIntegrationPresentationViewModel> Integrations { get; } = [];
    public ObservableCollection<CapabilityPresentationViewModel> Capabilities { get; } = [];
    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand EnableGloballyCommand { get; }
    public IAsyncRelayCommand DisableGloballyCommand { get; }
    public IAsyncRelayCommand EnableOnThisDeviceCommand { get; }
    public IAsyncRelayCommand DisableOnThisDeviceCommand { get; }
    public IAsyncRelayCommand InheritGlobalSettingCommand { get; }
    public IAsyncRelayCommand OpenCapabilityCommand { get; }
    public IRelayCommand BackToCatalogCommand { get; }
    public IRelayCommand ShowHomeCommand { get; }
    public IRelayCommand ShowSettingsCommand { get; }
    public bool IsHomeVisible => CurrentSurface == ShellSurface.Home;
    public bool IsSettingsVisible => CurrentSurface == ShellSurface.Settings;
    public bool IsReferenceCapabilityOpen => OpenedReferenceCapability is not null;
    public bool IsVocationOpportunityOverviewOpen => OpenedVocationOpportunityOverview is not null;
    public bool IsCapabilityDetailsVisible => !IsReferenceCapabilityOpen && !IsVocationOpportunityOverviewOpen;

    public Task EnsureInitializedAsync()
    {
        lock (initializationGate)
            return initializationTask ??= InitializeCoreAsync();
    }

    partial void OnSelectedIntegrationChanged(ServiceIntegrationPresentationViewModel? value)
    {
        RefreshCommandStates();
    }

    partial void OnSelectedCapabilityChanged(CapabilityPresentationViewModel? value)
    {
        RefreshCommandStates();
    }

    partial void OnOpenedReferenceCapabilityChanged(CapabilityPresentationViewModel? value)
    {
        OnPropertyChanged(nameof(IsReferenceCapabilityOpen));
        OnPropertyChanged(nameof(IsCapabilityDetailsVisible));
    }

    partial void OnOpenedVocationOpportunityOverviewChanged(VocationOpportunityOverviewViewModel? value)
    {
        OnPropertyChanged(nameof(IsVocationOpportunityOverviewOpen));
        OnPropertyChanged(nameof(IsCapabilityDetailsVisible));
    }

    partial void OnCurrentSurfaceChanged(ShellSurface value)
    {
        OnPropertyChanged(nameof(IsHomeVisible));
        OnPropertyChanged(nameof(IsSettingsVisible));
    }

    private async Task InitializeCoreAsync()
    {
        try
        {
            var device = await ensureCurrentDevice.GetOrCreateAsync(suggestedDeviceName);
            CurrentDeviceIdentity = device.DeviceIdentity;
            CurrentDeviceName = device.DisplayName;
            await registerKnownIntegrations.RegisterAsync();
            await RefreshCoreAsync();
        }
        catch
        {
            StatusText = "WGT could not load its local state.";
            throw;
        }
    }

    private async Task RefreshAsync()
    {
        await EnsureInitializedAsync();
        await RefreshCoreAsync();
    }

    private async Task RefreshCoreAsync()
    {
        if (CurrentDeviceIdentity is null) return;
        try
        {
            var results = await refreshPublications.RefreshAsync();
            var failures = results.Where(x => x.Status != IntegrationRefreshStatus.Refreshed).ToArray();
            StatusText = failures.Length == 0
                ? "Ready"
                : "Some integration publications could not be refreshed.";
            await ReloadStateAsync();
        }
        catch
        {
            StatusText = "WGT could not refresh its local state.";
            throw;
        }
    }

    private async Task ReloadStateAsync()
    {
        if (CurrentDeviceIdentity is null) return;
        var selectedServiceIdentity = SelectedIntegration?.ServiceIdentity;
        var integrations = await listServiceIntegrations.ListAsync(CurrentDeviceIdentity);
        Replace(Integrations, integrations.Select(item => new ServiceIntegrationPresentationViewModel(item)));
        SelectedIntegration = SelectedIntegration is not null
            ? Integrations.FirstOrDefault(x => x.ServiceIdentity == selectedServiceIdentity)
            : Integrations.FirstOrDefault();

        var entries = await resolveCapabilityCatalog.ResolveAsync(CurrentDeviceIdentity);
        var selectedId = SelectedCapability?.CapabilityIdentity;
        Replace(Capabilities, entries.Select(x => new CapabilityPresentationViewModel(x)));
        SelectedCapability = selectedId is null
            ? Capabilities.FirstOrDefault()
            : Capabilities.FirstOrDefault(x => x.CapabilityIdentity == selectedId);
        if (OpenedReferenceCapability is not null)
            OpenedReferenceCapability = Capabilities.FirstOrDefault(x => x.CapabilityIdentity == OpenedReferenceCapability.CapabilityIdentity && x.CanOpen);
        RefreshCommandStates();
    }

    private async Task EnableGloballyAsync() { if (SelectedIntegration is { } selected) { await setGlobalEnablement.EnableAsync(selected.ServiceIdentity); await ReloadStateAsync(); } }
    private async Task DisableGloballyAsync() { if (SelectedIntegration is { } selected) { await setGlobalEnablement.DisableAsync(selected.ServiceIdentity); await ReloadStateAsync(); } }
    private async Task EnableOnThisDeviceAsync() { if (SelectedIntegration is { } selected && CurrentDeviceIdentity is { } device) { await setDeviceOverride.SetAsync(selected.ServiceIdentity, device, true); await ReloadStateAsync(); } }
    private async Task DisableOnThisDeviceAsync() { if (SelectedIntegration is { } selected && CurrentDeviceIdentity is { } device) { await setDeviceOverride.SetAsync(selected.ServiceIdentity, device, false); await ReloadStateAsync(); } }
    private async Task InheritGlobalSettingAsync() { if (SelectedIntegration is { } selected && CurrentDeviceIdentity is { } device) { await clearDeviceOverride.ClearAsync(selected.ServiceIdentity, device); await ReloadStateAsync(); } }
    private async Task OpenCapabilityAsync()
    {
        if (SelectedCapability is not { CanOpen: true } capability) return;
        if (string.Equals(capability.CapabilityIdentity.Value, "reference.available", StringComparison.Ordinal))
        {
            OpenedVocationOpportunityOverview = null;
            OpenedReferenceCapability = capability;
            return;
        }

        if (string.Equals(capability.CapabilityIdentity.Value, "vocation.opportunity_overview", StringComparison.Ordinal) && readVocationOpportunityOverview is not null)
        {
            OpenedReferenceCapability = null;
            var viewModel = new VocationOpportunityOverviewViewModel(readVocationOpportunityOverview);
            OpenedVocationOpportunityOverview = viewModel;
            await viewModel.RefreshAsync();
        }
    }

    private bool CanManageSelectedIntegration() => SelectedIntegration is not null && CurrentDeviceIdentity is not null;
    private bool CanOpenSelectedCapability()
    {
        var selected = SelectedCapability;
        return selected?.CanOpen == true && (!string.Equals(selected.CapabilityIdentity.Value, "vocation.opportunity_overview", StringComparison.Ordinal) || readVocationOpportunityOverview is not null);
    }
    private void RefreshCommandStates()
    {
        EnableGloballyCommand.NotifyCanExecuteChanged();
        DisableGloballyCommand.NotifyCanExecuteChanged();
        EnableOnThisDeviceCommand.NotifyCanExecuteChanged();
        DisableOnThisDeviceCommand.NotifyCanExecuteChanged();
        InheritGlobalSettingCommand.NotifyCanExecuteChanged();
        OpenCapabilityCommand.NotifyCanExecuteChanged();
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source) target.Add(item);
    }
}
