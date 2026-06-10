using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;

namespace DOTS.Features.Encyclopedia.Models;

public class DotsInformationRestrictionModel : DefaultInformationRestrictionModel
{
    private readonly IEncyclopediaSettingsProvider _settings;

    public DotsInformationRestrictionModel(IEncyclopediaSettingsProvider settings)
    {
        _settings = settings;
    }

    public override bool DoesPlayerKnowDetailsOf(Hero hero)
    {
        if (_settings.ShowAllEncyclopediaCharacters)
            return true;
        return base.DoesPlayerKnowDetailsOf(hero);
    }
}
