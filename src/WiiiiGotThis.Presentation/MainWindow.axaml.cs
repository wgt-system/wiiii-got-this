using Avalonia.Controls;

namespace WiiiiGotThis.Presentation;

public sealed partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    public void ConfigureIlluminationProductSurfaceSource(IIlluminationProductSurfaceSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        ShellView.IlluminationProductSurfaceSource = source;
    }
}
