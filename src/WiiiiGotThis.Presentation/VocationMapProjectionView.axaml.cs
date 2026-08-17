using Avalonia.Controls;

namespace WiiiiGotThis.Presentation;

public partial class VocationMapProjectionView : UserControl
{
    public VocationMapProjectionView()
    {
        InitializeComponent();
    }

    public bool FocusPrimaryControl() => MapRefreshButton.IsVisible && MapRefreshButton.Focus();
}
