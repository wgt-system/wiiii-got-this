using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopShellView : UserControl
{
    private ShellViewModel? shell;
    private bool isAttached;

    public DesktopShellView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttached;
        DetachedFromVisualTree += OnDetached;
        DataContextChanged += OnDataContextChanged;
    }

    private async void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        isAttached = true;
        AttachShell(DataContext as ShellViewModel);
        var currentShell = shell;
        if (currentShell is not null)
            await currentShell.EnsureInitializedAsync();

        Dispatcher.UIThread.Post(FocusCurrentSurface);
    }

    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        isAttached = false;
        AttachShell(null);
    }

    private void OnDataContextChanged(object? sender, EventArgs e) => AttachShell(DataContext as ShellViewModel);

    private void AttachShell(ShellViewModel? next)
    {
        if (ReferenceEquals(shell, next))
            return;

        if (shell is not null)
            shell.PropertyChanged -= OnShellPropertyChanged;

        shell = next;
        if (shell is not null)
            shell.PropertyChanged += OnShellPropertyChanged;
    }

    private void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShellViewModel.CurrentSurface))
            Dispatcher.UIThread.Post(FocusCurrentSurface);
    }

    private void FocusCurrentSurface()
    {
        var currentShell = shell;
        if (!isAttached || currentShell is null)
            return;

        switch (currentShell.CurrentSurface)
        {
            case ShellSurface.Home:
                HomeNavigation.Focus();
                break;
            case ShellSurface.Jobs:
                if (!JobsWorkspace.FocusPrimaryControl())
                    JobsNavigation.Focus();
                break;
            case ShellSurface.Map:
                if (!MapWorkspace.FocusPrimaryControl())
                    MapNavigation.Focus();
                break;
            case ShellSurface.Settings:
                if (!SettingsConnectionsList.Focus())
                    SettingsNavigation.Focus();
                break;
        }
    }
}
