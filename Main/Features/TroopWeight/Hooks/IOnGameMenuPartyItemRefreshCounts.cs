using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;

namespace DOTS.Features.TroopWeight.Hooks;

public interface IOnGameMenuPartyItemRefreshCounts
{
    void OnGameMenuPartyItemRefreshCounts(GameMenuPartyItemVM partyItem);
}
