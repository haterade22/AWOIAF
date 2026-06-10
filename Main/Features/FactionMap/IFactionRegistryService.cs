using System.Collections.Generic;
using DOTS.Features.FactionMap.Models;

namespace DOTS.Features.FactionMap;

public interface IFactionRegistryService
{
    void Initialize(Dictionary<string, RegionData> regions, Dictionary<string, FactionData> factions);
    RegionData? GetRegion(string regionKey);
    FactionData? GetFactionForRegion(string regionKey);
    IEnumerable<string> GetAllRegionKeys();
    FactionData? GetFaction(string factionId);
}
