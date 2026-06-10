using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using DOTS.Features.CareerSystem;
using DOTS.Features.CareerSystem.Domain;

namespace DOTS.Features.CulturalFeats.Models;

public class DotsPartyMoraleModel : DefaultPartyMoraleModel
{
    private readonly ICulturalFeatsService _feats;
    private readonly ICareerPassiveService _careerPassives;

    public DotsPartyMoraleModel(ICulturalFeatsService feats, ICareerPassiveService careerPassives)
    {
        _feats = feats;
        _careerPassives = careerPassives;
    }

    public override ExplainedNumber GetEffectivePartyMorale(
        MobileParty party, bool includeDescription = false)
    {
        var result = base.GetEffectivePartyMorale(party, includeDescription);
        // Vanilla PartyBaseHelper.HasFeat precedence via the shared helper.
        _feats.ApplyMoraleFeats(CultureFeatAdapter.FromOrNull(party.Party), ref result);
        _careerPassives.ApplyFactor(party.LeaderHero?.StringId, ref result, PassiveEffectType.TroopMorale);
        return result;
    }
}
