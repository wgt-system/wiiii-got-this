using WiiiiGotThis.Application;

namespace WiiiiGotThis.Application.Tests;

public sealed class AtlasAppearanceContractTests
{
    [Fact]
    public void Atlas_exposes_the_four_renderer_slots_as_stable_appearance_choices()
    {
        var themes = Enum.GetValues<AtlasThemePreference>();

        Assert.Equal(
            [
                AtlasThemePreference.Technical,
                AtlasThemePreference.Elegant,
                AtlasThemePreference.Machine,
                AtlasThemePreference.World
            ],
            themes);
    }

    [Fact]
    public void Atlas_exposes_full_and_reduced_motion_as_presentation_preferences()
    {
        Assert.Equal(
            [AtlasMotionPreference.Full, AtlasMotionPreference.Reduced],
            Enum.GetValues<AtlasMotionPreference>());
    }
}
