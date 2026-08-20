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

    public void ConfigureProductRuntimes(
        IVocationProductRuntime vocationRuntime,
        IOrientationProductRuntime orientationRuntime)
    {
        ArgumentNullException.ThrowIfNull(vocationRuntime);
        ArgumentNullException.ThrowIfNull(orientationRuntime);
        ShellView.VocationProductRuntime = vocationRuntime;
        ShellView.OrientationProductRuntime = orientationRuntime;
    }
}
