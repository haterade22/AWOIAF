using TaleWorlds.CampaignSystem;

namespace DOTS.Adapters;

public class QuestHeroAdapterFactory : IQuestHeroAdapterFactory
{
    public IQuestHeroAdapter Create(Hero hero)
    {
        return hero != null ? new QuestHeroAdapter(hero) : null;
    }
}
