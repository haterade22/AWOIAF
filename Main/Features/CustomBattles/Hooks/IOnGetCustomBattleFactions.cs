using System.Collections.Generic;
using TaleWorlds.Core;

namespace DOTS.Features.CustomBattles.Hooks;

public interface IOnGetCustomBattleFactions
{
    void OnGetCustomBattleFactions(ref IEnumerable<BasicCultureObject> factions);
}
