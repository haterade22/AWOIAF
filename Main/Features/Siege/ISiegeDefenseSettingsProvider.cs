namespace DOTS.Features.Siege;

public interface ISiegeDefenseSettingsProvider
{
    bool EnableSiegeDefenseEvents { get; }
    int SiegeDefenseResponseDays { get; }
}
