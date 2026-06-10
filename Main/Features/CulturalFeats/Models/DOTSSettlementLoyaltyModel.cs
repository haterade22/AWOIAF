using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using DOTS.Features.RevoltTuning;

namespace DOTS.Features.CulturalFeats.Models;

public class DotsSettlementLoyaltyModel : DefaultSettlementLoyaltyModel
{
    private readonly RevoltTuningConfig _revoltConfig;
    private readonly ICulturalFeatsService _feats;

    public DotsSettlementLoyaltyModel(
        ICulturalFeatsService feats,
        IRevoltTuningConfigProvider revoltTuning)
    {
        _feats = feats;
        _revoltConfig = revoltTuning.GetConfig();
    }

    public override int RebellionStartLoyaltyThreshold =>
        _revoltConfig.RebellionStartLoyaltyThreshold;

    public override int RebelliousStateStartLoyaltyThreshold =>
        _revoltConfig.RebelliousStateStartLoyaltyThreshold;

    public override float SettlementOwnerDifferentCultureLoyaltyEffect =>
        _revoltConfig.SettlementOwnerDifferentCultureLoyaltyEffect;

    public override float GovernorDifferentCultureLoyaltyEffect =>
        _revoltConfig.GovernorDifferentCultureLoyaltyEffect;

    public override ExplainedNumber CalculateLoyaltyChange(Town town, bool includeDescriptions = false)
    {
        var result = base.CalculateLoyaltyChange(town, includeDescriptions);
        _feats.ApplyLoyaltyFeats(CultureFeatAdapter.FromOrNull(town.Owner?.Culture), ref result);
        return result;
    }
}
