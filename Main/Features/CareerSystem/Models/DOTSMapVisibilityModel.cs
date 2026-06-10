using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using DOTS.Features.CareerSystem.Domain;

namespace DOTS.Features.CareerSystem.Models;

public class DotsMapVisibilityModel : DefaultMapVisibilityModel
{
    private readonly ICareerPassiveService _careerPassives;

    public DotsMapVisibilityModel(ICareerPassiveService careerPassives)
    {
        _careerPassives = careerPassives;
    }

    public override ExplainedNumber GetPartySpottingRange(MobileParty party, bool includeDescriptions = false)
    {
        var result = base.GetPartySpottingRange(party, includeDescriptions);
        _careerPassives.ApplyFactor(party?.LeaderHero?.StringId, ref result, PassiveEffectType.PartySpottingRange);
        return result;
    }
}
