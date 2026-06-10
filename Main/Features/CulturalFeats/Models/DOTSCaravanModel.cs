using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;

namespace DOTS.Features.CulturalFeats.Models;

public class DotsCaravanModel : DefaultCaravanModel
{
    private readonly ICulturalFeatsService _feats;

    public DotsCaravanModel(ICulturalFeatsService feats)
    {
        _feats = feats;
    }

    public override int GetCaravanFormingCost(bool largerCaravan, bool navalCaravan)
        => _feats.ApplyCaravanCost(
            CultureFeatAdapter.FromOrNull(CharacterObject.PlayerCharacter?.Culture),
            base.GetCaravanFormingCost(largerCaravan, navalCaravan));
}
