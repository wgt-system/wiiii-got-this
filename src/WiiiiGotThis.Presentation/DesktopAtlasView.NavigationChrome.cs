using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace WiiiiGotThis.Presentation;

public sealed partial class DesktopAtlasView
{
    private Button? atlasCenterButton;

    private void EnsureAtlasNavigationChrome()
    {
        if (atlasCenterButton is not null || ThemeMenuButton.Parent is not Canvas settingsCanvas)
            return;

        var icon = new Canvas
        {
            Width = 20,
            Height = 20,
            IsHitTestVisible = false
        };
        var ring = new Border
        {
            Width = 12,
            Height = 12,
            CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(1),
            IsHitTestVisible = false
        };
        ring.Classes.Add("wgt-atlas-center-ring");
        Canvas.SetLeft(ring, 4);
        Canvas.SetTop(ring, 4);
        icon.Children.Add(ring);

        var dot = new Border
        {
            Width = 3,
            Height = 3,
            CornerRadius = new CornerRadius(2),
            IsHitTestVisible = false
        };
        dot.Classes.Add("wgt-atlas-center-dot");
        Canvas.SetLeft(dot, 8.5);
        Canvas.SetTop(dot, 8.5);
        icon.Children.Add(dot);

        icon.Children.Add(CenterTick(1, 4, 9.5, 0));
        icon.Children.Add(CenterTick(1, 4, 9.5, 16));
        icon.Children.Add(CenterTick(4, 1, 0, 9.5));
        icon.Children.Add(CenterTick(4, 1, 16, 9.5));

        atlasCenterButton = new Button
        {
            Width = 34,
            Height = 34,
            CornerRadius = new CornerRadius(17),
            Padding = new Thickness(0),
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Content = icon
        };
        atlasCenterButton.Classes.Add("wgt-atlas-center-control");
        ToolTip.SetTip(atlasCenterButton, "Fit WGT Atlas");
        AutomationProperties.SetName(atlasCenterButton, "Fit WGT Atlas");
        AutomationProperties.SetAutomationId(atlasCenterButton, "AtlasCenterWgt");
        atlasCenterButton.Click += OnCenterAtlas;
        Canvas.SetRight(atlasCenterButton, 50);
        Canvas.SetTop(atlasCenterButton, 3);
        settingsCanvas.Children.Add(atlasCenterButton);

        QueueInitialOverviewFit();
    }

    private static Border CenterTick(double width, double height, double left, double top)
    {
        var tick = new Border
        {
            Width = width,
            Height = height,
            IsHitTestVisible = false
        };
        tick.Classes.Add("wgt-atlas-center-tick");
        Canvas.SetLeft(tick, left);
        Canvas.SetTop(tick, top);
        return tick;
    }

    private void OnCenterAtlas(object? sender, RoutedEventArgs e)
    {
        if (shell?.AtlasSettingsExpanded == true && shell.ToggleAtlasSettingsCommand.CanExecute(null))
            shell.ToggleAtlasSettingsCommand.Execute(null);
        ThemeChoices.IsVisible = false;
        shell?.SelectAtlasNodeCommand.Execute(null);

        if (!FitOverviewCamera())
            ResetCamera();

        AtlasViewport.Focus();
        e.Handled = true;
    }
}
