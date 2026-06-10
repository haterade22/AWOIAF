using DryIoc;

namespace DOTS.Features.MixedFormations;

public static class MixedFormationsIoC
{
    public static void RegisterMixedFormationsFeature(IContainer container)
    {
        container.Register<IMixedFormationsSettingsProvider, MixedFormationsSettingsProvider>(Reuse.Singleton);
        container.Register<ILayoutPositioner, LayoutPositioner>(Reuse.Singleton);
        container.Register<IFormationLayoutService, FormationLayoutService>(Reuse.Singleton);
    }
}
