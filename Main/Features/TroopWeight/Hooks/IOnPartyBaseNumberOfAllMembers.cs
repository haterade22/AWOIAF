using TaleWorlds.CampaignSystem.Party;

namespace DOTS.Features.TroopWeight.Hooks;

public interface IOnPartyBaseNumberOfAllMembers
{
    void OnPartyBaseNumberOfAllMembers(PartyBase partyBase, ref int __result);
}
