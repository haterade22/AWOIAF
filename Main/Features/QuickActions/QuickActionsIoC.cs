using DryIoc;
using DOTS.Adapters;
using DOTS.Features.QuickActions.Audio;
using DOTS.Features.QuickActions.Hooks;

namespace DOTS.Features.QuickActions;

public static class QuickActionsIoC
{
    public static void RegisterQuickActionsFeature(IContainer container)
    {
        container.Register<IQuickActionsSettingsProvider, QuickActionsSettingsProvider>(Reuse.Singleton);
        container.Register<IInventoryVMAdapter, InventoryVMAdapter>(Reuse.Singleton);
        container.Register<IQuickActionsAudioPlayer, QuickActionsAudioPlayer>(Reuse.Singleton);
        container.Register<InventorySearchCampaignBehavior>(Reuse.Singleton);
        container.Register<IQuickActionsService, QuickActionsService>(Reuse.Singleton);
    }
}
