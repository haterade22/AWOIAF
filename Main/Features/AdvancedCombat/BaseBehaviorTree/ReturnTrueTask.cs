using BehaviorTrees;
using BehaviorTrees.Nodes;

namespace DOTS.Features.AdvancedCombat.BaseBehaviorTree;

public class ReturnTrueTask : BTTask
{
    public override BTTaskStatus Execute()
    {
        return BTTaskStatus.FinishedWithTrue;
    }
}
