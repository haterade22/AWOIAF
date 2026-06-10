using TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;

namespace DOTS.Features.CompanionTactics.FormationPresets;

public sealed class OrderOfBattleVMTracker : IOrderOfBattleVMTracker
{
    public OrderOfBattleVM Current { get; private set; }

    public void SetCurrent(OrderOfBattleVM vm) => Current = vm;
    public void ClearCurrent() => Current = null;
}
