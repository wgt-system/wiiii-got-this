using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopShellView : UserControl
{
    private ShellViewModel? shell;

    public DesktopShellView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttached;
        DetachedFromVisualTree += OnDetached;
        DataContextChanged += OnDataContextChanged;
    }

    private async void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        AttachShell(DataContext as ShellViewModel);
        if (shell is not null)
            await shell.EnsureInitializedAsync();

        Dispatcher.UIThread.Post(FocusCurrentSurface);
    }

    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) => AttachShell(null);

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
        if (shell is null || !IsAttachedToVisualTree())
            return;

        switch (shell.CurrentSurface)
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
