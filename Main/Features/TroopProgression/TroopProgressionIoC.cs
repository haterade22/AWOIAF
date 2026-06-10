using DryIoc;
using DOTS.Adapters;

namespace DOTS.Features.TroopProgression;

public static class TroopProgressionIoC
{
    public static void RegisterTroopProgressionFeature(IContainer container)
    {
        container.Register<ITroopCostService, TroopCostService>(Reuse.Singleton);
        container.Register<IWageModifierService, WageModifierService>(Reuse.Singleton);
        container.Register<IVolunteerTierService, VolunteerTierService>(Reuse.Singleton);
        container.Register<IRandomProvider, RandomProvider>(Reuse.Singleton);
        container.Register<IVolunteerRecruitmentService, VolunteerRecruitmentService>(Reuse.Singleton);
        // Phase 9b #148 P2 — moved from global Main/IoC.cs; only consumer is DotsVolunteerModel
        // in this feature, so registration belongs here (cohesion).
        container.Register<IVolunteerContextAdapter, VolunteerContextAdapter>(Reuse.Singleton);
    }
}
