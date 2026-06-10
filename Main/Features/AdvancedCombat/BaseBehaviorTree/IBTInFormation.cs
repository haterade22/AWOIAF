using BehaviorTrees;
using TaleWorlds.MountAndBlade;

namespace DOTS.Features.AdvancedCombat.BaseBehaviorTree;

internal interface IBTInFormation : IBTBlackboard
{
    BTBlackboardValue<Formation> AgentsFormation { get; set; }
}
