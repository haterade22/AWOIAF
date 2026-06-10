using TaleWorlds.CampaignSystem.Party;

namespace DOTS.Features.TroopWeight.Hooks;

public interface IOnPartyBaseNumberOfRegularMembers
{
    void OnPartyBaseNumberOfRegularMembers(PartyBase partyBase, ref int __result);
}
