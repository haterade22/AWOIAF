using TaleWorlds.CampaignSystem.Settlements;

namespace DOTS.Adapters;

public interface IRemoteFiefSettlementSwapper
{
    bool ReflectionTargetAvailable { get; }
    Settlement Swap(Settlement target);
    void Restore(Settlement original);
}
