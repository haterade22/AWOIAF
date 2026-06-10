using BehaviorTrees;
using BehaviorTreeWrapper.BlackBoardClasses;
using TaleWorlds.MountAndBlade;

namespace DOTS.Features.AdvancedCombat.BaseBehaviorTree;

public interface IBTMountBase : IBTBannerlordBase
{
    BTBlackboardValue<Agent> Rider { get; set; }
}
