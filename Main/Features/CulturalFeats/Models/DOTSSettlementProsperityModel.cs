using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace DOTS.Features.CulturalFeats.Models;

public class DotsSettlementProsperityModel : DefaultSettlementProsperityModel
{
    private readonly ICulturalFeatsService _feats;

    public DotsSettlementProsperityModel(ICulturalFeatsService feats)
    {
        _feats = feats;
    }

    public override ExplainedNumber CalculateHearthChange(Village village, bool includeDescriptions = false)
    {
        var result = base.CalculateHearthChange(village, includeDescriptions);
        _feats.ApplyHearthGrowthFeats(CultureFeatAdapter.FromOrNull(village.Settlement?.OwnerClan?.Culture), ref result);
        return result;
    }
}
