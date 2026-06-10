using TaleWorlds.CampaignSystem.CharacterCreationContent;

namespace DOTS.Features.CharacterCreation;

public interface ICareerMenuService
{
    void RegisterCareerMenu(CharacterCreationManager manager);
    string SelectedCareerStringId { get; }
}
