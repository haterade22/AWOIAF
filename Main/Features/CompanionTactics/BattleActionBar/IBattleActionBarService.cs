using System.Collections.Generic;
using DOTS.Adapters;
using DOTS.Features.CompanionTactics.BattleActionBar.Models;

namespace DOTS.Features.CompanionTactics.BattleActionBar;

public interface IBattleActionBarService
{
    /// <summary>Resolve the contextual buttons for the formation, gated by MCM settings.</summary>
    IReadOnlyList<BattleAction> GetActionsForFormation(IFormationAdapter formation);
}
