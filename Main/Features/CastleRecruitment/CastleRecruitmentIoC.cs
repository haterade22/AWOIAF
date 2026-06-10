using DryIoc;

namespace DOTS.Features.CastleRecruitment;

public static class CastleRecruitmentIoC
{
    public static void RegisterCastleRecruitmentFeature(IContainer container)
    {
        container.Register<ICastleRecruitmentConfigProvider, CastleRecruitmentConfigProvider>(Reuse.Singleton);
        container.Register<ICastleRecruitmentSettingsProvider, CastleRecruitmentSettingsProvider>(Reuse.Singleton);
        container.Register<ICastleRecruitmentService, CastleRecruitmentService>(Reuse.Singleton);
    }
}
