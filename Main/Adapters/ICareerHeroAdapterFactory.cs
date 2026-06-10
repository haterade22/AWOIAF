using TaleWorlds.CampaignSystem;

namespace DOTS.Adapters;

public interface ICareerHeroAdapterFactory
{
    ICareerHeroAdapter Create(Hero hero);
}
