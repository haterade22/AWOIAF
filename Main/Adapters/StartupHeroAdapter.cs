using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace DOTS.Adapters;

public class StartupHeroAdapter : IStartupHeroAdapter
{
    public IReadOnlyList<LordHeroInfo> GetAliveLordHeroes()
    {
        var results = new List<LordHeroInfo>();

        foreach (var hero in Hero.AllAliveHeroes)
        {
            if (hero.Occupation != Occupation.Lord)
                continue;

            results.Add(new LordHeroInfo(
                hero.StringId,
                hero.Culture?.StringId ?? "",
                hero.Clan == Clan.PlayerClan));
        }

        return results;
    }
}
