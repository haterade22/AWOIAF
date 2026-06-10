using TaleWorlds.MountAndBlade;

namespace DOTS.Adapters;

public interface IMissionAdapterFactory
{
    IAgentAdapter GetAgentAdapter(Agent agent);
    void ClearCache();
}
