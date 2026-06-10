using TaleWorlds.CampaignSystem;

namespace DOTS.Features.InitialChildGeneration;

public class DotsInitialChildGenerationBehavior : CampaignBehaviorBase
{
    private readonly IInitialChildGenerationService _service;

    public DotsInitialChildGenerationBehavior(IInitialChildGenerationService service)
    {
        _service = service;
    }

    public override void RegisterEvents()
    {
        CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(
            this, OnNewGameCreatedPartialFollowUp);
    }

    public void OnNewGameCreatedPartialFollowUp(CampaignGameStarter starter, int index)
    {
        if (index == 0)
            _service.GenerateInitialChildren();
    }

    public override void SyncData(IDataStore dataStore) { }
}
