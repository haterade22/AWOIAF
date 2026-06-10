using System.Collections.Generic;
using DOTS.Features.Siege.Models;

namespace DOTS.Features.Siege;

public class SiegeEngineAvailabilityService : ISiegeEngineAvailabilityService
{
    public IEnumerable<DefenderSiegeEngineKind> GetDefenderEngines(bool hasFirePerks)
    {
        yield return DefenderSiegeEngineKind.Ballista;
        if (hasFirePerks) yield return DefenderSiegeEngineKind.FireBallista;
        yield return DefenderSiegeEngineKind.Catapult;
        if (hasFirePerks) yield return DefenderSiegeEngineKind.FireCatapult;
        yield return DefenderSiegeEngineKind.Trebuchet;
    }
}
