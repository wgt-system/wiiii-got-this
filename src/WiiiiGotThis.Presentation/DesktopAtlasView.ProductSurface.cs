using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopAtlasView
{
    private Grid? vocationProductOverlay;
    private VocationProductView? vocationProductView;

    private void OnOpenProductSurface(object? sender, RoutedEventArgs e)
    {
        if (shell?.SelectedAtlasNode?.CanOpenProductSurface != true)
            return;

        EnsureVocationProductOverlay();
        vocationProductOverlay!.IsVisible = true;
        vocationProductView!.Reload();
        e.Handled = true;
    }

    private void EnsureVocationProductOverlay()
    {
        if (vocationProductOverlay is not null)
            return;

        vocationProductView = new VocationProductView();
        var returnButton = new Button
        {
            Width = 44,
            Height = 44,
            CornerRadius = new Avalonia.CornerRadius(22),
            Padding = new Avalonia.Thickness(0),
            Content = "◎",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Margin = new Avalonia.Thickness(18)
        };
        ToolTip.SetTip(returnButton, "Return to WGT Atlas");
        AutomationProperties.SetName(returnButton, "Return to WGT Atlas");
        AutomationProperties.SetAutomationId(returnButton, "ReturnToAtlasFromVocation");
        returnButton.Click += OnReturnFromProductSurface;

        vocationProductOverlay = new Grid
        {
            Background = new SolidColorBrush(Colors.Transparent),
            IsVisible = false,
            Children =
            {
                vocationProductView,
                returnButton
            }
        };
        AutomationProperties.SetName(vocationProductOverlay, "Vocation in Wiiii Got This");
        AtlasViewport.Children.Add(vocationProductOverlay);
    }

    private void OnReturnFromProductSurface(object? sender, RoutedEventArgs e)
    {
        if (vocationProductOverlay is not null)
            vocationProductOverlay.IsVisible = false;
        AtlasSearch.Focus();
        e.Handled = true;
    }
}
