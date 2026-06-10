using System.Collections.Generic;
using DOTS.Features.FactionMap.Models;

namespace DOTS.Features.FactionMap;

public interface IFactionConfigProvider
{
    Dictionary<string, RegionData> LoadRegions();
    Dictionary<string, FactionData> LoadFactions();
}
