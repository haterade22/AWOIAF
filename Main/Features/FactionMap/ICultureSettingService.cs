using TaleWorlds.CampaignSystem;

namespace DOTS.Features.FactionMap;

public interface ICultureSettingService
{
    void SetCultureOnCharacterCreation(CultureObject culture, object viewInstance, object? originalDataSource);
}
