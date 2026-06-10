using System.Collections.Generic;
using DOTS.Features.SpecialResources.Domain;

namespace DOTS.Features.SpecialResources;

public interface ISpecialResourceConfigProvider
{
    IReadOnlyList<SpecialResource> GetAllResources();
    SpecialResource GetByKingdomId(string kingdomId);
    SpecialResource GetByCultureId(string cultureId);
    TroopResourceCostEntry GetTroopCost(string troopId);
}
