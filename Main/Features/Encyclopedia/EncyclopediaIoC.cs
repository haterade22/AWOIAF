using DryIoc;

namespace DOTS.Features.Encyclopedia;

public static class EncyclopediaIoC
{
    public static void RegisterEncyclopediaFeature(IContainer container)
    {
        container.Register<IEncyclopediaSettingsProvider, EncyclopediaSettingsProvider>(Reuse.Singleton);
    }
}
