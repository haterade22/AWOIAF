using TaleWorlds.CampaignSystem;

namespace DOTS.Adapters;

public class CareerHeroAdapterFactory : ICareerHeroAdapterFactory
{
    public ICareerHeroAdapter Create(Hero hero)
    {
        return hero != null ? new CareerHeroAdapter(hero) : null;
    }
}
