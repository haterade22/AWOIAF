using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using DOTS.Features.CareerSystem;
using DOTS.Features.CareerSystem.Domain;

namespace DOTS.Features.CulturalFeats.Models;

public class DotsRaidModel : DefaultRaidModel
{
    private readonly ICulturalFeatsService _feats;
    private readonly ICareerPassiveService _careerPassives;

    public DotsRaidModel(ICulturalFeatsService feats, ICareerPassiveService careerPassives)
    {
        _feats = feats;
        _careerPassives = careerPassives;
    }

    public override ExplainedNumber CalculateHitDamage(
        MapEventSide attackerSide, float settlementHitPoints)
    {
        var result = base.CalculateHitDamage(attackerSide, settlementHitPoints);
        _feats.ApplyRaidDamageFeats(
            CultureFeatAdapter.FromOrNull(attackerSide?.LeaderParty?.Owner?.Culture),
            ref result);
        _careerPassives.ApplyFactor(attackerSide?.LeaderParty?.Owner?.StringId, ref result, PassiveEffectType.TroopDamage);
        return result;
    }
}
