using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using DOTS.Features.CareerSystem;
using DOTS.Features.CareerSystem.Domain;

namespace DOTS.Features.CulturalFeats.Models;

public class DotsPartyTroopUpgradeModel : DefaultPartyTroopUpgradeModel
{
    private readonly ICulturalFeatsService _feats;
    private readonly ICareerPassiveService _careerPassives;

    public DotsPartyTroopUpgradeModel(ICulturalFeatsService feats, ICareerPassiveService careerPassives)
    {
        _feats = feats;
        _careerPassives = careerPassives;
    }

    public override ExplainedNumber GetGoldCostForUpgrade(
        PartyBase party, CharacterObject characterObject, CharacterObject upgradeTarget)
    {
        var result = base.GetGoldCostForUpgrade(party, characterObject, upgradeTarget);
        // Vanilla PartyBaseHelper.HasFeat precedence via the shared CultureFeatAdapter helper —
        // same fix Codex 43 made to speed model and the 3-pack RCA applied to size model.
        _feats.ApplyTroopUpgradeFeats(
            CultureFeatAdapter.FromOrNull(party),
            characterObject.IsMounted,
            ref result);
        _careerPassives.ApplyFactor((party.Owner ?? party.LeaderHero)?.StringId, ref result, PassiveEffectType.TroopUpgradeCost);
        return result;
    }
}
