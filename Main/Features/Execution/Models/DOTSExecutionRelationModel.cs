using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using DOTS.Adapters;

namespace DOTS.Features.Execution.Models;

public class DotsExecutionRelationModel : DefaultExecutionRelationModel
{
    private readonly IExecutionRelationService _service;
    private readonly IPlayerContextAdapter _playerContext;

    public DotsExecutionRelationModel(IExecutionRelationService service, IPlayerContextAdapter playerContext)
    {
        _service = service;
        _playerContext = playerContext;
    }

    public override int GetRelationChangeForExecutingHero(Hero victim, Hero hero, out bool showQuickNotification)
    {
        // Boundary: convert sealed TaleWorlds heroes to string kingdom IDs + compute vanilla baseline.
        int baseDelta = base.GetRelationChangeForExecutingHero(victim, hero, out bool baseShowNotification);
        var executorKingdomId = _playerContext.GetPlayerKingdomId();
        var victimKingdomId = victim?.Clan?.Kingdom?.StringId;
        var evaluatorKingdomId = hero?.Clan?.Kingdom?.StringId;

        // Delegate: all decisions live in IExecutionRelationService.
        var result = _service.GetRelationModifier(
            executorKingdomId,
            victimKingdomId,
            evaluatorKingdomId,
            baseDelta,
            baseShowNotification);

        showQuickNotification = result.ShowNotification;
        return result.RelationDelta;
    }
}
