using DOTS.Features;

namespace DOTS.Features.Siege;

public class SiegeDefenseSettingsProvider : ISiegeDefenseSettingsProvider
{
    public bool EnableSiegeDefenseEvents => DotsSettings.Instance?.EnableSiegeDefenseEvents ?? true;
    public int SiegeDefenseResponseDays => DotsSettings.Instance?.SiegeDefenseResponseDays ?? 3;
}
