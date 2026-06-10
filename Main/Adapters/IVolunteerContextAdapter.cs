using TaleWorlds.CampaignSystem;
using DOTS.Features.TroopProgression;

namespace DOTS.Adapters;

public interface IVolunteerContextAdapter
{
    VolunteerContext GetContext(Hero hero);
    CharacterObject ResolveCharacter(string characterId);
}
