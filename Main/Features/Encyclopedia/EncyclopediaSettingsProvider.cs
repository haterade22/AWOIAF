namespace DOTS.Features.Encyclopedia;

/// <summary>
/// Production wire-up of <see cref="IEncyclopediaSettingsProvider"/> reading from the MCM
/// <c>DotsSettings</c> singleton. Phase 9b #145.
/// </summary>
public class EncyclopediaSettingsProvider : IEncyclopediaSettingsProvider
{
    public bool ShowAllEncyclopediaCharacters => DotsSettings.Instance?.ShowAllEncyclopediaCharacters ?? true;
}
