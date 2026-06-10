using DOTS.Features.SettlementGuards.Domain;

namespace DOTS.Features.SettlementGuards;

public interface ISettlementGuardService
{
    string ResolveGuardTroopId(SettlementGuardContext context, string spawnPointTag);
    string ResolveSpearItemId(string cultureId);
}
