using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;

namespace DOTS.Features.CulturalFeats.Models;

public class DotsSettlementMilitiaModel : DefaultSettlementMilitiaModel
{
    private readonly ICulturalFeatsService _feats;

    public DotsSettlementMilitiaModel(ICulturalFeatsService feats)
    {
        _feats = feats;
    }

    public override ExplainedNumber CalculateVeteranMilitiaSpawnChance(Settlement settlement)
    {
        var result = base.CalculateVeteranMilitiaSpawnChance(settlement);
        _feats.ApplyVeteranMilitiaFeats(CultureFeatAdapter.FromOrNull(settlement.OwnerClan?.Culture), ref result);
        return result;
    }
}
