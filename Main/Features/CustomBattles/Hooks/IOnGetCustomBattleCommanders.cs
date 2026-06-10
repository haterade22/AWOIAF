using System.Collections.Generic;
using TaleWorlds.Core;

namespace DOTS.Features.CustomBattles.Hooks;

public interface IOnGetCustomBattleCommanders
{
    void OnGetCustomBattleCommanders(ref IEnumerable<BasicCharacterObject> commanders);
}
