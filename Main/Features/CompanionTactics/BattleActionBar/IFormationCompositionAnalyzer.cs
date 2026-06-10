using DOTS.Adapters;
using DOTS.Features.CompanionTactics.BattleActionBar.Models;

namespace DOTS.Features.CompanionTactics.BattleActionBar;

public interface IFormationCompositionAnalyzer
{
    FormationComposition Analyze(IFormationAdapter formation);
}
