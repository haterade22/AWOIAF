using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace DOTS.Features.TroopWeight.Hooks;

public interface IOnPartyBaseHelperGetPartySizeText
{
    void OnGetPartySizeText(PartyBase party, ref TextObject __result);
}
