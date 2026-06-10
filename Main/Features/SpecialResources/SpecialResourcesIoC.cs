using DryIoc;
using DOTS.Features.SpecialResources.Hooks;

namespace DOTS.Features.SpecialResources;

public static class SpecialResourcesIoC
{
    public static void RegisterSpecialResourcesFeature(IContainer container)
    {
        container.Register<ISpecialResourceConfigProvider, SpecialResourceConfigProvider>(Reuse.Singleton);
        container.Register<ISpecialResourceStorageService, SpecialResourceStorageService>(Reuse.Singleton);
        container.Register<ISpecialResourceService, SpecialResourceService>(Reuse.Singleton);
        container.Register<IOnPartyUpgradeResourceCheck, PartyUpgradeResourceCheckHook>(Reuse.Singleton);
    }
}
