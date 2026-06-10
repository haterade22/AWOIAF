using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using DOTS.Features.CulturalFeats;

namespace DOTS.Features.Diplomacy.Models;

public class DotsDiplomacyModel : DefaultDiplomacyModel
{
    private readonly IWarOfTheRingService _wotrService;

    public DotsDiplomacyModel(IWarOfTheRingService wotrService)
    {
        _wotrService = wotrService;
    }

    public override bool IsAtConstantWar(IFaction faction1, IFaction faction2)
    {
        if (_wotrService.IsWarOfTheRingActive
            && _wotrService.ShouldBlockPeace(faction1.StringId, faction2.StringId))
        {
            return true;
        }

        return base.IsAtConstantWar(faction1, faction2);
    }

    public override int GetRelationChangeAfterVotingInSettlementOwnerPreliminaryDecision(
        Hero supporter, bool hasHeroVotedAgainstOwner)
    {
        int result = base.GetRelationChangeAfterVotingInSettlementOwnerPreliminaryDecision(
            supporter, hasHeroVotedAgainstOwner);

        if (hasHeroVotedAgainstOwner
            && supporter.Culture?.HasFeat(DotsCulturalFeats.IsengardDecisionPenaltyFeat) == true)
        {
            result += (int)(result * DotsCulturalFeats.IsengardDecisionPenaltyFeat.EffectBonus);
        }

        return result;
    }
}
