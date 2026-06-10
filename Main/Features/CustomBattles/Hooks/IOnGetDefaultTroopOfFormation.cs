using TaleWorlds.Core;

namespace DOTS.Features.CustomBattles.Hooks;

public interface IOnGetDefaultTroopOfFormation
{
    void OnGetDefaultTroopOfFormation(string cultureId, int formationIndex, ref BasicCharacterObject result);
}
