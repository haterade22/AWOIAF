using System.Collections.Generic;
using DOTS.Features.Siege.Models;

namespace DOTS.Features.Siege;

public interface ISiegeEngineAvailabilityService
{
    IEnumerable<DefenderSiegeEngineKind> GetDefenderEngines(bool hasFirePerks);
}
