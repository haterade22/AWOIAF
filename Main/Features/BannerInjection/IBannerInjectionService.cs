using TaleWorlds.CampaignSystem;

namespace DOTS.Features.BannerInjection;

public interface IBannerInjectionService
{
    void InjectBanners();
    void SyncData(IDataStore dataStore);
}
