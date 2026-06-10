using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace DOTS.Adapters;

public interface IAgentVisualsAdapter
{
    Skeleton GetSkeleton();
    MatrixFrame GetGlobalFrame();
}
