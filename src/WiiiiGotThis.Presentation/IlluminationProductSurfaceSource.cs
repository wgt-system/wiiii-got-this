using Avalonia.Controls;

namespace WiiiiGotThis.Presentation;

public interface IIlluminationProductSurfaceSource
{
    Task<Control> CreateAsync();
}
