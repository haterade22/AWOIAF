using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core.ViewModelCollection.Information;

namespace DOTS.Features.TroopWeight.Hooks;

public interface IOnCampaignUIHelperGetPartyHealthTooltip
{
    void OnGetPartyHealthTooltip(PartyBase party, ref List<TooltipProperty> __result);
}
