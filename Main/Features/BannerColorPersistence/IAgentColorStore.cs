using DOTS.Adapters;

namespace DOTS.Features.BannerColorPersistence;

public interface IAgentColorStore
{
    void Register(int agentIndex, ClanColorInfo info);
    bool TryGetColors(int agentIndex, out ClanColorInfo info);
    void Clear();
}
