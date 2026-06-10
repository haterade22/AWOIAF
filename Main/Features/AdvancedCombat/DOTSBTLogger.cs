using BehaviorTrees;
using DOTS.Core.Logging;

namespace DOTS.Features.AdvancedCombat;

public class DotsBTLogger : ILogger
{
    private static IModLogger _logger => IoC.Resolve<IModLogger>();

    public void LogMessage(string message)
    {
        _logger.LogError(message);
    }
}
