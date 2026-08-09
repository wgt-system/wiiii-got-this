using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace WiiiiGotThis.Presentation;

public sealed partial class MobileShellView : UserControl
{
    public MobileShellView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttached;
    }

    private async void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is ShellViewModel shell) await shell.EnsureInitializedAsync();
    }
}
