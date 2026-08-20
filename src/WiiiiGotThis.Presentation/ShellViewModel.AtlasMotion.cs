using CommunityToolkit.Mvvm.ComponentModel;
using WiiiiGotThis.Application;

namespace WiiiiGotThis.Presentation;

public sealed partial class ShellViewModel
{
    private Task? atlasMotionLoadTask;

    [ObservableProperty]
    private AtlasMotionPreference atlasMotion = AtlasMotionPreference.Full;

    public bool IsAtlasReducedMotion => AtlasMotion == AtlasMotionPreference.Reduced;

    public async Task SetAtlasMotionAsync(
        AtlasMotionPreference motion,
        CancellationToken cancellationToken = default)
    {
        AtlasMotion = motion;
        if (writeAtlasAppearance is null)
            return;

        try
        {
            await writeAtlasAppearance.SetMotionAsync(motion, cancellationToken);
        }
        catch (IOException)
        {
            StatusText = "Motion preference changed for this session but could not be saved.";
        }
        catch (UnauthorizedAccessException)
        {
            StatusText = "Motion preference changed for this session but could not be saved.";
        }
    }

    partial void OnAtlasMotionChanged(AtlasMotionPreference value) =>
        OnPropertyChanged(nameof(IsAtlasReducedMotion));

    partial void OnCurrentDeviceNameChanged(string value)
    {
        if (readAtlasAppearance is not null)
            atlasMotionLoadTask ??= LoadAtlasMotionPreferenceAsync();
    }

    private async Task LoadAtlasMotionPreferenceAsync()
    {
        var stored = await readAtlasAppearance!.GetMotionAsync();
        AtlasMotion = stored ?? AtlasMotionPreference.Full;
    }
}
