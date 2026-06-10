using DryIoc;
using DOTS.Adapters;
using DOTS.Features.BannerInjection.Hooks;

namespace DOTS.Features.BannerInjection;

public static class BannerInjectionIoC
{
    public static void RegisterBannerInjectionFeature(IContainer container)
    {
        container.Register<IKingdomBannerAdapter, KingdomBannerAdapter>(Reuse.Singleton);
        container.Register<IClanBannerAdapter, ClanBannerAdapter>(Reuse.Singleton);
        container.Register<IBannerExclusionService, BannerExclusionService>(Reuse.Singleton);
        container.Register<IBannerConfigProvider, BannerConfigProvider>(Reuse.Singleton);
        container.Register<IBannerInjectionService, BannerInjectionService>(Reuse.Singleton);

        container.Register<IOnBannerEditorDone, BannerEditorDoneHook>(Reuse.Singleton);
        var hook = container.Resolve<IOnBannerEditorDone>();
        GauntletBannerEditorScreen_OnDone_Patch.Initialize(hook);
    }
}
