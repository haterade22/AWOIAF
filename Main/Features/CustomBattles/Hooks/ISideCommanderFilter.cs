using System.Collections.Generic;
using TaleWorlds.Core;

namespace DOTS.Features.CustomBattles.Hooks;

public interface ISideCommanderFilter
{
    IReadOnlyList<BasicCharacterObject> ResolveCommandersForCulture(string cultureId);
}
