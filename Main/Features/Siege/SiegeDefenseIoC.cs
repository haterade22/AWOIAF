using DryIoc;
using DOTS.Adapters;

namespace DOTS.Features.Siege;

public static class SiegeDefenseIoC
{
    public static void RegisterSiegeDefenseFeature(IContainer container)
    {
        container.Register<ISiegeDefenseConfigProvider, SiegeDefenseConfigProvider>(Reuse.Singleton);
        container.Register<ISiegeDefenseSettingsProvider, SiegeDefenseSettingsProvider>(Reuse.Singleton);
        container.Register<IPlayerContextAdapter, PlayerContextAdapter>(Reuse.Singleton);
        container.Register<ISiegeDefenseService, SiegeDefenseService>(Reuse.Singleton);
        container.Register<ISiegeEngineAvailabilityService, SiegeEngineAvailabilityService>(Reuse.Singleton);
    }
}
