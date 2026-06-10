using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;

namespace DOTS.Features.CulturalFeats.Models;

public class DotsBuildingConstructionModel : DefaultBuildingConstructionModel
{
    private readonly ICulturalFeatsService _feats;

    public DotsBuildingConstructionModel(ICulturalFeatsService feats)
    {
        _feats = feats;
    }

    public override ExplainedNumber CalculateDailyConstructionPower(Town town, bool includeDescriptions = false)
    {
        var result = base.CalculateDailyConstructionPower(town, includeDescriptions);
        _feats.ApplyConstructionSpeedFeats(CultureFeatAdapter.FromOrNull(town.OwnerClan?.Culture), ref result);
        return result;
    }
}
