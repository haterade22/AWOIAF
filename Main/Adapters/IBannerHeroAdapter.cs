using TaleWorlds.CampaignSystem;

namespace DOTS.Adapters;

public interface IBannerHeroAdapter
{
    ClanColorInfo? GetClanColorInfo(CharacterObject characterObject);
    ClanColorInfo? GetClanColorInfoFromHero(Hero hero);
    void SyncKingdomColors(Clan clan);
}
