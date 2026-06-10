using DryIoc;
using DOTS.Adapters;

namespace DOTS.Features.SiegeDismount;

public static class SiegeDismountIoC
{
    public static void RegisterSiegeDismountFeature(IContainer container)
    {
        container.Register<ISiegeDismountSettingsProvider, SiegeDismountSettingsProvider>(Reuse.Singleton);
        container.Register<IPlayerMountAdapter, PlayerMountAdapter>(Reuse.Singleton);
        container.Register<IPartyMountInventoryAdapter, PartyMountInventoryAdapter>(Reuse.Singleton);
        container.Register<ISiegeDismountService, SiegeDismountService>(Reuse.Singleton);
    }
}
