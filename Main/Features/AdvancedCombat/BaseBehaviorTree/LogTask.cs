using BehaviorTrees;
using BehaviorTrees.Nodes;
using DOTS.Core.Logging;

namespace DOTS.Features.AdvancedCombat.BaseBehaviorTree;

public class LogTask : BTTask
{
    private readonly string _message;
    private static IModLogger Logger => IoC.Resolve<IModLogger>();

    public LogTask(string message)
    {
        _message = message;
    }

    public override BTTaskStatus Execute()
    {
        Logger.LogWarning(_message);
        return BTTaskStatus.FinishedWithTrue;
    }
}
