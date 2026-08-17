using Avalonia.Controls;

namespace WiiiiGotThis.Presentation;

public sealed partial class VocationOpportunityOverviewView : UserControl
{
    public VocationOpportunityOverviewView() => InitializeComponent();

    public bool FocusPrimaryControl() => JobsSearchBox.IsVisible && JobsSearchBox.Focus();
}
