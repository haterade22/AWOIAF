using DryIoc;
using DOTS.Features.AdvancedCombat.Services;

namespace DOTS.Features.AdvancedCombat;

public static class AdvancedCombatIoC
{
    public static void RegisterAdvancedCombatFeature(IContainer container)
    {
        container.Register<IBoneCollisionService, BoneCollisionService>(Reuse.Singleton);
        container.Register<ISpatialGridDebugService, SpatialGridDebugService>(Reuse.Singleton);
    }
}
