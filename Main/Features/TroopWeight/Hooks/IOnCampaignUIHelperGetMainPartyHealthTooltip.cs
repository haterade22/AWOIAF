using System.Collections.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;

namespace DOTS.Features.TroopWeight.Hooks;

public interface IOnCampaignUIHelperGetMainPartyHealthTooltip
{
    void OnGetMainPartyHealthTooltip(ref List<TooltipProperty> __result);
}
