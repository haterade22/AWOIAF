using DOTS.Features.MixedFormations.Models;

namespace DOTS.Features.MixedFormations;

public class MixedFormationsSettingsProvider : IMixedFormationsSettingsProvider
{
    public bool IsEnabled => DotsSettings.Instance?.EnableMixedFormations ?? true;

    public FormationLayoutType DefaultLayout =>
        ResolveLayout(DotsSettings.Instance?.MixedFormationsDefaultLayout ?? 0);

    public string CycleHotkey => DotsSettings.Instance?.MixedFormationsCycleHotkey ?? "L";

    public bool IsDebugMode => DotsSettings.Instance?.MixedFormationsDebug ?? false;

    private static FormationLayoutType ResolveLayout(int raw) => raw switch
    {
        0 => FormationLayoutType.InfantryFrontRangedBack,
        1 => FormationLayoutType.RangedFrontInfantryBack,
        2 => FormationLayoutType.RangedWingsInfantryCenter,
        3 => FormationLayoutType.Checkerboard,
        _ => FormationLayoutType.InfantryFrontRangedBack,
    };
}
