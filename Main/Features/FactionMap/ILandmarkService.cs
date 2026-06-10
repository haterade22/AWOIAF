using System.Collections.Generic;
using DOTS.Features.FactionMap.Models;

namespace DOTS.Features.FactionMap;

public interface ILandmarkService
{
    IReadOnlyList<LandmarkDef> GetAllLandmarks();
    IEnumerable<LandmarkDef> GetCapitals();
    IEnumerable<LandmarkDef> GetByFaction(int factionId);
}
